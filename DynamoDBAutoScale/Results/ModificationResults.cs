using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynamoDBAutoScale.Results
{
	public class ModificationResults
	{
		public bool Success
		{
			get { return SuccessfulUpdates.Any() && !Errors.Any(); }
			set { Success = value; }
		}
		public List<string> SuccessfulUpdates { get; set; }
		public List<Tuple<string, Exception>> Errors { get; set; }

		public ModificationResults()
		{
			this.SuccessfulUpdates = new List<string>();
			this.Errors = new List<Tuple<string, Exception>>();
		}

		public string ToString()
		{
			StringBuilder string_builder = new StringBuilder();

			if (Success)
				string_builder.AppendFormat("{0} Items Updated Successfully", SuccessfulUpdates.Count);
			else
			{
				if (SuccessfulUpdates.Any())
				{
					string_builder.Append("Successful Updates:").AppendLine();
					SuccessfulUpdates.ForEach(successful_update =>
					{
						string_builder.Append(successful_update).AppendLine();
					});
					string_builder.AppendLine();
				}

				string_builder.Append("Errors:").AppendLine();
				Errors.ForEach(error =>
				{
					string_builder.AppendFormat("{0} - {1}", error.Item1, error.Item2.Message).AppendLine();
				});
			}

			return string_builder.ToString().Trim();
		}
	}
}