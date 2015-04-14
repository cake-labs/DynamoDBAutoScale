using System.Collections.Generic;
using System.Text;
using Amazon.DynamoDBv2.Model;
using DynamoDBAutoScale.Results;

namespace DynamoDBAutoScale
{
	public class ModifiedThroughput
	{
		public string table_name { get; set; }
		public string index_name { get; set; }
		public ProvisionedThroughputDescription current_provisioned_throughput { get; set; }
		public ProvisionedThroughput new_provisioned_throughput { get; set; }
		public List<ThroughputModificationResult> throughput_modification_results { get; set; }

		public ModifiedThroughput(string table_name, string index_name, ProvisionedThroughputDescription current_provisioned_throughput)
		{
			this.table_name = table_name;
			this.index_name = index_name;
			this.current_provisioned_throughput = current_provisioned_throughput;
			this.new_provisioned_throughput = null;
			this.throughput_modification_results = new List<ThroughputModificationResult>();
		}

		public string ToString(bool debug = false)
		{
			StringBuilder string_builder = new StringBuilder();

			string_builder.AppendFormat("Table Name: {0}", table_name).AppendLine();
			if (!string.IsNullOrEmpty(index_name))
				string_builder.AppendFormat("Index Name: {0}", index_name).AppendLine();

			string_builder.Append("Current Provisioned Throughput:").AppendLine();
			string_builder.AppendFormat("\tRead Capacity Units : {0}", current_provisioned_throughput.ReadCapacityUnits).AppendLine();
			string_builder.AppendFormat("\tWrite Capacity Units : {0}", current_provisioned_throughput.WriteCapacityUnits).AppendLine();

			if (new_provisioned_throughput != null)
			{
				string_builder.Append("New Provisioned Throughput:").AppendLine();
				string_builder.AppendFormat("\tRead Capacity Units : {0}", new_provisioned_throughput.ReadCapacityUnits).AppendLine();
				string_builder.AppendFormat("\tWrite Capacity Units : {0}", new_provisioned_throughput.WriteCapacityUnits).AppendLine();
			}

			throughput_modification_results.ForEach(throughput_modification_result =>
			{
				string_builder.Append(throughput_modification_result.ToString(debug));
			});

			return string_builder.ToString();
		}
	}
}