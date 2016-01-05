using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using DynamoDBAutoScale.ConfigMapping;
using DynamoDBAutoScale.Results;

namespace DynamoDBAutoScale
{
	public static class AutoScale
	{
		public static PreparedModifications BuildModificatons(string rules_config_file_path = null, bool include_non_changes = false)
		{
			try
			{
				PreparedModifications prepared_modifications = new PreparedModifications();
				Rules rules = LoadRules(rules_config_file_path);
				List<ThroughputModification> throughput_modifications = rules.GenerateThroughputModifications();

				if (throughput_modifications.Any())
				{
					AmazonDynamoDBClient amazon_dynamo_db_client = AWS.GetAmazonDynamoDBClient();
					ListTablesResponse list_table_response = amazon_dynamo_db_client.ListTables();
					List<string> table_names = list_table_response.TableNames;
					table_names.ForEach(table_name =>
					{
						// find all throughput modifications that have applicable rules (both table/index level) for this table name
						List<ThroughputModification> qualified_throughput_modifications = throughput_modifications.Where(throughput_modification => throughput_modification.Qualify(table_name)).ToList();
						if (qualified_throughput_modifications.Any())
						{
							DescribeTableResponse describe_table_response = amazon_dynamo_db_client.DescribeTable(table_name);
							TableDescription table = describe_table_response.Table;

							// find all throughput modifications that have applicable rules at the table level only
							List<ThroughputModification> qualified_table_throughput_modifications = qualified_throughput_modifications
								.Where(throughput_modification => throughput_modification.QualifyTable(table_name))
								.OrderByDescending(throughput_modification => throughput_modification.GetTableMatchScore(table_name))
								.ThenByDescending(throughput_modification => throughput_modification.index_id)
								.ToList();

							if (qualified_table_throughput_modifications.Any())
							{
								ModifiedThroughput modified_throughput = BuildModifiedThroughput(table_name, null, table.ProvisionedThroughput, qualified_table_throughput_modifications);
								if (include_non_changes || modified_throughput.new_provisioned_throughput != null)
									prepared_modifications.modified_throughputs.Add(modified_throughput);
							}

							List<GlobalSecondaryIndexDescription> global_secondary_indexes = table.GlobalSecondaryIndexes;
							global_secondary_indexes.ForEach(global_secondary_index =>
							{
								// find all throughput modifications that have applicable rules at the index level only
								string index_name = global_secondary_index.IndexName;
								List<ThroughputModification> qualified_index_throughput_modifications = qualified_throughput_modifications
									.Where(throughput_modification => throughput_modification.QualifyIndex(table_name, index_name))
									.OrderByDescending(throughput_modfication => throughput_modfication.GetIndexMatchScore(table_name, index_name))
									.ThenByDescending(throughput_modification => throughput_modification.index_id)
									.ToList();

								if (qualified_index_throughput_modifications.Any())
								{
									ModifiedThroughput modified_throughput = BuildModifiedThroughput(table_name, index_name, global_secondary_index.ProvisionedThroughput, qualified_index_throughput_modifications);
									if (include_non_changes || modified_throughput.new_provisioned_throughput != null)
										prepared_modifications.modified_throughputs.Add(modified_throughput);
								}
							});
						}
					});
				}

				return prepared_modifications;
			}
			catch (Exception exception)
			{
				// provide custom exception handling here
				throw;
			}
		}

		private static Rules LoadRules(string rules_config_file_path = null)
		{
			Rules rules = new Rules();

			if (string.IsNullOrEmpty(rules_config_file_path)) // if no file path is provided, load from app.config default location
				rules = (Rules)ConfigurationManager.GetSection("Rules");
			else if (File.Exists(rules_config_file_path)) // else if a file path is provided and the file exists, load the rules from the override file
			{
				using (StreamReader stream_reader = new StreamReader(rules_config_file_path))
				{
					XmlSerializer xml_serializer = new XmlSerializer(typeof(Rules));
					rules = (Rules)xml_serializer.Deserialize(stream_reader);
				}
			}

			return rules;
		}

		private static ModifiedThroughput BuildModifiedThroughput(string table_name, string index_name, ProvisionedThroughputDescription current_provisioned_throughput, List<ThroughputModification> throughput_modifications)
		{
			ModifiedThroughput modified_throughput = new ModifiedThroughput(table_name, index_name, current_provisioned_throughput);
			long? new_read_capacity_units = null;
			long? new_write_capacity_units = null;

			for (int index = 0;
				index < throughput_modifications.Count && (!new_read_capacity_units.HasValue || !new_write_capacity_units.HasValue);
				index++)
			{
				ThroughputModification throughput_modification = throughput_modifications.ElementAt(index);
				ThroughputModificationResult throughput_modification_result = throughput_modification.ApplyRule(table_name, index_name, new_read_capacity_units, new_write_capacity_units, current_provisioned_throughput);
				modified_throughput.throughput_modification_results.Add(throughput_modification_result);

				if (throughput_modification_result.read_result != null)
				{
					new_read_capacity_units = throughput_modification_result.read_result.new_capacity_units;
					modified_throughput.read_basic_alarm = (ReadBasicAlarm)throughput_modification.read.basic_alarm;
				}
				if (throughput_modification_result.write_result != null)
				{
					new_write_capacity_units = throughput_modification_result.write_result.new_capacity_units;
					modified_throughput.write_basic_alarm = (WriteBasicAlarm)throughput_modification.write.basic_alarm;
				}
			}

			new_read_capacity_units = new_read_capacity_units ?? current_provisioned_throughput.ReadCapacityUnits;
			new_write_capacity_units = new_write_capacity_units ?? current_provisioned_throughput.WriteCapacityUnits;
			if (new_read_capacity_units != current_provisioned_throughput.ReadCapacityUnits || new_write_capacity_units != current_provisioned_throughput.WriteCapacityUnits)
			{
				modified_throughput.new_provisioned_throughput = new ProvisionedThroughput
				{
					ReadCapacityUnits = new_read_capacity_units.Value,
					WriteCapacityUnits = new_write_capacity_units.Value
				};
			}

			return modified_throughput;
		}

		public static ModificationResults ApplyModifications(PreparedModifications prepared_modifications, bool continue_on_error = true)
		{
			ModificationResults modification_results = new ModificationResults();

			if (prepared_modifications.modified_throughputs.Any())
			{
				var table_name_groups = prepared_modifications.modified_throughputs
					.Where(modified_throughput => modified_throughput.new_provisioned_throughput != null)
					.GroupBy(modified_throughput => modified_throughput.table_name)
					.ToList();

				foreach (var table_name_group in table_name_groups)
				{
					try
					{
						UpdateTableRequest update_table_request = new UpdateTableRequest { TableName = table_name_group.Key };
						update_table_request.ProvisionedThroughput = null;
						update_table_request.GlobalSecondaryIndexUpdates = new List<GlobalSecondaryIndexUpdate>();

						ModifiedThroughput table_modified_throughput = table_name_group.SingleOrDefault(modified_throughput => string.IsNullOrEmpty(modified_throughput.index_name));
						if (table_modified_throughput != null)
							update_table_request.ProvisionedThroughput = table_modified_throughput.new_provisioned_throughput;

						List<ModifiedThroughput> index_modified_throughputs = table_name_group.Where(modified_throughput => !string.IsNullOrEmpty(modified_throughput.index_name)).ToList();
						foreach (var index_modified_throughput in index_modified_throughputs)
						{
							GlobalSecondaryIndexUpdate global_secondary_index_update = new GlobalSecondaryIndexUpdate
							{
								Update = new UpdateGlobalSecondaryIndexAction
								{
									IndexName = index_modified_throughput.index_name,
									ProvisionedThroughput = index_modified_throughput.new_provisioned_throughput
								}
							};
							update_table_request.GlobalSecondaryIndexUpdates.Add(global_secondary_index_update);
						}

						AmazonDynamoDBClient amazon_dynamo_db_client = AWS.GetAmazonDynamoDBClient();
						amazon_dynamo_db_client.UpdateTable(update_table_request);
						modification_results.SuccessfulUpdates.Add(table_name_group.Key);

						if (table_modified_throughput != null)
						{
							if (table_modified_throughput.read_basic_alarm != null)
								table_modified_throughput.read_basic_alarm.Enable(table_name_group.Key, table_modified_throughput.new_provisioned_throughput.ReadCapacityUnits);
							if (table_modified_throughput.write_basic_alarm != null)
								table_modified_throughput.write_basic_alarm.Enable(table_name_group.Key, table_modified_throughput.new_provisioned_throughput.WriteCapacityUnits);
						}
					}
					catch (Exception exception)
					{
						modification_results.Errors.Add(new Tuple<string, Exception>(table_name_group.Key, exception));
						if (!continue_on_error)
							break;
					}
				}
			}

			return modification_results;
		}
	}
}