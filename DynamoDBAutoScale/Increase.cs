using System;
using System.Linq;
using Amazon.DynamoDBv2.Model;
using DynamoDBAutoScale.Enumerations;
using DynamoDBAutoScale.Results;

namespace DynamoDBAutoScale
{
	public class Increase : IncreaseDecrease
	{
		public Increase(ConfigMapping.IncreaseDecrease increase_decrease) : base(increase_decrease) { }

		#region abstract methods

		protected override bool ThresholdMet(long consumed_capacity_units, long current_capacity_units)
		{
			if (this.threshold.measurement_type == MeasurementTypes.Units)
			{
				long threshold = current_capacity_units - this.threshold.measurement_units;
				return consumed_capacity_units >= threshold;
			}
			else // if (this.threshold_measurement_type == MeasurementTypes.Percentage)
			{
				byte consumed_capacity_percentage = (byte)(Math.Ceiling((((double)consumed_capacity_units / (double)current_capacity_units) * 100)));
				return consumed_capacity_percentage >= (100 - this.threshold.measurement_percentage);
			}
		}

		protected override bool IsBlockedOut(ProvisionedThroughputDescription current_provisioned_throughput)
		{
			bool is_blocked_out = false;

			DateTime utc_now = DateTime.UtcNow;
			is_blocked_out = this.blockout_time_frames.Any(blockout_time_frame => utc_now >= blockout_time_frame.Item1 && utc_now < blockout_time_frame.Item2);

			return is_blocked_out;
		}

		protected override long GetNewCapacityUnits(long capacity_units)
		{
			if (this.amount.measurement_type == MeasurementTypes.Units)
				return capacity_units + this.amount.measurement_units;
			else // if (this.amount_measurement_type == MeasurementTypes.Percentage)
				return capacity_units + (long)Math.Ceiling(capacity_units * ((float)this.amount.measurement_percentage / 100));
		}

		#endregion
	}
}