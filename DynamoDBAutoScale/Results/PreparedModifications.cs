using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynamoDBAutoScale.Results
{
	public class PreparedModifications
	{
		public List<ModifiedThroughput> modified_throughputs { get; set; }

		public PreparedModifications()
		{
			this.modified_throughputs = new List<ModifiedThroughput>();
		}

		public string ToString(bool debug = false)
		{
			StringBuilder string_builder = new StringBuilder();

			modified_throughputs.ForEach(modified_throughput =>
			{
				string_builder.AppendLine();
				string_builder.Append("--------------------------------------------------").AppendLine();
				string_builder.AppendLine();
				string_builder.Append(modified_throughput.ToString(debug));
			});

			return string_builder.ToString().Trim();
		}
	}
}