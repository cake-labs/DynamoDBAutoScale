using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynamoDBAutoScale.Results
{
	public class ReadWriteResult
	{
		public long consumed_capacity_units { get; set; }
		public int look_back_minutes { get; set; }

		public IncreaseDecreaseResult increase_result { get; set; }
		public IncreaseDecreaseResult decrease_result { get; set; }
		public IncreaseDecreaseResult decrease_combination_modifier_result { get; set; }

		public bool maximum_throughput_reached { get; set; }
		public bool minimum_throughput_reached { get; set; }
		public long? new_capacity_units { get; set; }

		public void ApplyAdjustmentConstraints(long? minimum_throughput, long? maximum_throughput)
		{
			if (new_capacity_units.HasValue)
			{
				if (minimum_throughput.HasValue && new_capacity_units.Value < minimum_throughput.Value)
				{
					minimum_throughput_reached = true;
					new_capacity_units = minimum_throughput.Value;
				}
				if (maximum_throughput.HasValue && new_capacity_units.Value > maximum_throughput.Value)
				{
					maximum_throughput_reached = true;
					new_capacity_units = maximum_throughput.Value;
				}
			}
		}

		public string ToString(bool debug = false)
		{
			StringBuilder string_builder = new StringBuilder();

			if (debug)
				string_builder.AppendFormat("\t\tLook Back Minutes: {0}", look_back_minutes).AppendLine();
			string_builder.AppendFormat("\t\tConsumed Capacity Units: {0}", consumed_capacity_units).AppendLine();

			if (increase_result != null)
			{
				string_builder.Append("\t\tIncrease:").AppendLine();
				string_builder.Append(increase_result.ToString(debug));
			}

			if (decrease_result != null)
			{
				string_builder.Append("\t\tDecrease:").AppendLine();
				string_builder.Append(decrease_result.ToString(debug));
			}

			if (decrease_combination_modifier_result != null)
			{
				string_builder.Append("\t\tDecrease Combination Modifier:").AppendLine();
				string_builder.Append(decrease_combination_modifier_result.ToString(debug));
			}

			if (new_capacity_units.HasValue)
			{
				if (debug || maximum_throughput_reached)
					string_builder.AppendFormat("\t\tMaximum Throughput Reached: {0}", maximum_throughput_reached).AppendLine();
				if (debug || minimum_throughput_reached)
					string_builder.AppendFormat("\t\tMinimum Throughput Reached: {0}", minimum_throughput_reached).AppendLine();
				string_builder.AppendFormat("\t\tFinal New Capacity Units: {0}", new_capacity_units.Value).AppendLine();
			}

			return string_builder.ToString();
		}
	}
}