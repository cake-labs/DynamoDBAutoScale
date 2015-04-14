using System.Text;
using DynamoDBAutoScale.Enumerations;

namespace DynamoDBAutoScale.Results
{
	public class IncreaseDecreaseResult
	{
		public Measurement threshold { get; set; }
		public Measurement amount { get; set; }
		public bool threshold_met { get; set; }
		public bool blocked_out { get; set; }
		public long? new_capacity_units { get; set; }

		public IncreaseDecreaseResult(Measurement threshold, Measurement amount, bool threshold_met, bool blocked_out, long? new_capacity_units)
		{
			this.threshold = threshold;
			this.amount = amount;
			this.threshold_met = threshold_met;
			this.blocked_out = blocked_out;
			this.new_capacity_units = new_capacity_units;
		}

		public string ToString(bool debug = false)
		{
			StringBuilder string_builder = new StringBuilder();

			if (debug)
			{
				string threshold_string = (threshold.measurement_type == MeasurementTypes.Units
					? string.Format("{0} Units", threshold.measurement_units)
					: string.Format("{0} %", threshold.measurement_percentage));
				string_builder.AppendFormat("\t\t\tThreshold: {0}", threshold_string).AppendLine();

				string amount_string = (amount.measurement_type == MeasurementTypes.Units
					? string.Format("{0} Units", amount.measurement_units)
					: string.Format("{0} %", amount.measurement_percentage));
				string_builder.AppendFormat("\t\t\tAmount: {0}", amount_string).AppendLine();
			}

			string_builder.AppendFormat("\t\t\tThreshold Met: {0}", threshold_met).AppendLine();
			if (debug || threshold_met)
			{
				if (debug || blocked_out)
					string_builder.AppendFormat("\t\t\tBlocked Out: {0}", blocked_out).AppendLine();

				if (new_capacity_units.HasValue && (debug || !blocked_out))
					string_builder.AppendFormat("\t\t\tNew Capacity Units: {0}", new_capacity_units.Value).AppendLine();
			}

			return string_builder.ToString();
		}
	}
}