using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynamoDBAutoScale.Results
{
	public class ThroughputModificationResult
	{
		public int index_id { get; set; }
		public ReadWriteResult read_result { get; set; }
		public ReadWriteResult write_result { get; set; }

		public ThroughputModificationResult(int index_id)
		{
			this.index_id = index_id;
		}

		public string ToString(bool debug = false)
		{
			StringBuilder string_builder = new StringBuilder();

			string_builder.AppendLine();
			string_builder.AppendFormat("Rule {0}:", index_id + 1).AppendLine();

			if (read_result != null)
			{
				string_builder.Append("\tRead:").AppendLine();
				string_builder.Append(read_result.ToString(debug));
			}

			if (write_result != null)
			{
				string_builder.Append("\tWrite:").AppendLine();
				string_builder.Append(write_result.ToString(debug));
			}

			return string_builder.ToString();
		}
	}
}