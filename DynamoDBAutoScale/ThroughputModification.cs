using System;
using System.Text.RegularExpressions;
using Amazon.DynamoDBv2.Model;
using DynamoDBAutoScale.ConfigMapping;
using DynamoDBAutoScale.Enumerations;
using System.Collections.Generic;
using DynamoDBAutoScale.Results;

namespace DynamoDBAutoScale
{
	public class ThroughputModification
	{
		public int index_id { get; set; }
		public int look_back_minutes { get; set; }
		public DecreaseFrequencies decrease_frequency { get; set; }
		public int decrease_frequency_custom_minutes { get; set; }
		public ExcludesIncludes excludes { get; set; }
		public ExcludesIncludes includes { get; set; }
		public Read read { get; set; }
		public Write write { get; set; }

		private const int look_back_minutes_default = 10;
		private const DecreaseFrequencies decrease_frequency_default = DecreaseFrequencies.EvenSpread;

		public ThroughputModification(int index_id, Rule rule)
		{
			// order id
			this.index_id = index_id;

			// look back minutes
			this.look_back_minutes = look_back_minutes_default;
			if (rule.look_back_minutes.HasValue)
				this.look_back_minutes = rule.look_back_minutes.Value;

			// decrease frequency
			this.decrease_frequency = decrease_frequency_default;
			this.decrease_frequency_custom_minutes = 0;
			if (!string.IsNullOrWhiteSpace(rule.decrease_frequency))
			{
				try
				{
					rule.decrease_frequency = Regex.Replace(rule.decrease_frequency, @"\s+", string.Empty).ToLower();
					if (rule.decrease_frequency == DecreaseFrequencies.EvenSpread.ToString().ToLower())
						this.decrease_frequency = DecreaseFrequencies.EvenSpread;
					else if (rule.decrease_frequency == DecreaseFrequencies.Immediate.ToString().ToLower())
						this.decrease_frequency = DecreaseFrequencies.Immediate;
					else
					{
						this.decrease_frequency = DecreaseFrequencies.Custom;
						int decrease_frequency_int = 0;
						bool invalid_custom_decrease_frequency = false;

						if (rule.decrease_frequency.EndsWith("hours") || rule.decrease_frequency.EndsWith("hour"))
						{
							rule.decrease_frequency = rule.decrease_frequency.Replace("hours", string.Empty).Replace("hour", string.Empty).Trim();
							int.TryParse(rule.decrease_frequency, out decrease_frequency_int);
							this.decrease_frequency_custom_minutes = decrease_frequency_int * 60;
						}
						else if (rule.decrease_frequency.EndsWith("minutes") || rule.decrease_frequency.EndsWith("minute"))
						{
							rule.decrease_frequency = rule.decrease_frequency.Replace("minutes", string.Empty).Replace("minute", string.Empty).Trim();
							int.TryParse(rule.decrease_frequency, out decrease_frequency_int);
							this.decrease_frequency_custom_minutes = decrease_frequency_int;
						}
						else
							invalid_custom_decrease_frequency = true;

						if (invalid_custom_decrease_frequency || decrease_frequency_custom_minutes > (60 * 24))
						{
							this.decrease_frequency = decrease_frequency_default;
							this.decrease_frequency_custom_minutes = 0;
						}
					}
				}
				catch (Exception exception)
				{
					// provide custom exception handling here
				}
			}

			// excludes & includes
			this.excludes = new ExcludesIncludes(rule.excludes);
			this.includes = new ExcludesIncludes(rule.includes);

			// read & write
			this.read = (rule.read != null ? new Read(rule.read, decrease_frequency, decrease_frequency_custom_minutes) : null);
			this.write = (rule.write != null ? new Write(rule.write, decrease_frequency, decrease_frequency_custom_minutes) : null);
		}

		public bool Qualify(string table_name)
		{
			return (!excludes.MatchesTable(table_name) && includes.MatchesTable(table_name)) // target table used in the tables list
				|| (!excludes.MatchesIndexTable(table_name) && includes.MatchesIndexTable(table_name)); // target table used in the indexes list
		}

		public bool QualifyTable(string table_name)
		{
			return !excludes.MatchesTable(table_name) && includes.MatchesTable(table_name); // target table used in the tables list
		}

		public bool QualifyIndex(string table_name, string index_name)
		{
			return !excludes.MatchesIndex(table_name, index_name) && includes.MatchesIndex(table_name, index_name); // target table and index used in the indexes list
		}

		public int GetTableMatchScore(string table_name)
		{
			return includes.GetTableMatchScore(table_name);
		}

		public int GetIndexMatchScore(string table_name, string index_name)
		{
			return includes.GetIndexMatchScore(table_name, index_name);
		}

		public ThroughputModificationResult ApplyRule(string table_name, string index_name, long? new_read_capacity_units, long? new_write_capacity_units, ProvisionedThroughputDescription current_provisioned_throughput)
		{
			ThroughputModificationResult throughput_modification_result = new ThroughputModificationResult(index_id);

			long current_read_capacity_units = current_provisioned_throughput.ReadCapacityUnits;
			long current_write_capacity_units = current_provisioned_throughput.WriteCapacityUnits;

			// if no new read capacity units has been determined and the current rule is set up with a read section, try to apply read rule
			if (!new_read_capacity_units.HasValue && read != null)
			{
				throughput_modification_result.read_result = read.ApplyRule(table_name, index_name, look_back_minutes, current_provisioned_throughput);
				new_read_capacity_units = throughput_modification_result.read_result.new_capacity_units;
			}

			// if no new write capacity units has been determined and the current rule is set up with a write section, try to apply write rule
			if (!new_write_capacity_units.HasValue && write != null)
			{
				throughput_modification_result.write_result = write.ApplyRule(table_name, index_name, look_back_minutes, current_provisioned_throughput);
				new_write_capacity_units = throughput_modification_result.write_result.new_capacity_units;
			}

			// look for decrease combination modifier eligibility
			if (
				(new_write_capacity_units.HasValue && new_write_capacity_units.Value < current_write_capacity_units && !new_read_capacity_units.HasValue && read != null)
				|| (new_read_capacity_units.HasValue && new_read_capacity_units.Value < current_read_capacity_units && !new_write_capacity_units.HasValue && write != null)
			)
			{
				if (new_write_capacity_units.HasValue && new_write_capacity_units.Value < current_write_capacity_units && !new_read_capacity_units.HasValue && read != null)
				{
					throughput_modification_result.read_result.decrease_combination_modifier_result = read.GetNewDecreaseCombinationModifierCapacityUnits(current_provisioned_throughput);
					if (throughput_modification_result.read_result.decrease_combination_modifier_result != null)
					{
						new_read_capacity_units = throughput_modification_result.read_result.decrease_combination_modifier_result.new_capacity_units;
						throughput_modification_result.read_result.new_capacity_units = new_read_capacity_units;
					}
				}
				else // if (new_read_capacity_units.HasValue && new_read_capacity_units.Value < current_read_capacity_units && !new_write_capacity_units.HasValue && write != null)
				{
					throughput_modification_result.write_result.decrease_combination_modifier_result = write.GetNewDecreaseCombinationModifierCapacityUnits(current_provisioned_throughput);
					if (throughput_modification_result.write_result.decrease_combination_modifier_result != null)
					{
						new_write_capacity_units = throughput_modification_result.write_result.decrease_combination_modifier_result.new_capacity_units;
						throughput_modification_result.write_result.new_capacity_units = new_write_capacity_units;
					}
				}
			}

			// apply minimum/maximum throughput configured in the rule
			if (throughput_modification_result.read_result != null)
				throughput_modification_result.read_result.ApplyAdjustmentConstraints(read.minimum_throughput, read.maximum_throughput);
			if (throughput_modification_result.write_result != null)
				throughput_modification_result.write_result.ApplyAdjustmentConstraints(write.minimum_throughput, write.maximum_throughput);

			return throughput_modification_result;
		}
	}
}