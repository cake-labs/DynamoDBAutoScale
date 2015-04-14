using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Amazon.DynamoDBv2.Model;
using DynamoDBAutoScale.Enumerations;

namespace DynamoDBAutoScale
{
	public class Decrease : IncreaseDecrease
	{
		private const long MaxNumberOfDecreasesDeafult = 4;

		private long max_number_of_decreases { get; set; }
		private DecreaseFrequencies decrease_frequency { get; set; }
		private int decrease_frequency_custom_minutes { get; set; }

		public Decrease(ConfigMapping.IncreaseDecrease increase_decrease, DecreaseFrequencies decrease_frequency, int decrease_frequency_custom_minutes) : base(increase_decrease)
		{
			this.decrease_frequency = decrease_frequency;
			this.decrease_frequency_custom_minutes = decrease_frequency_custom_minutes;
			
			string configured_max_number_of_decreases = ConfigurationManager.AppSettings.Get("MaxNumberOfDecreases");
			this.max_number_of_decreases = (!string.IsNullOrEmpty(configured_max_number_of_decreases)
				? long.Parse(configured_max_number_of_decreases)
				: MaxNumberOfDecreasesDeafult);
		}

		public Decrease(Decrease decrease, Measurement decrease_combination_modifier)
		{
			this.threshold = new Measurement(decrease.threshold);
			this.threshold.measurement_units -= decrease_combination_modifier.measurement_units;
			this.threshold.measurement_percentage -= decrease_combination_modifier.measurement_percentage;

			this.amount = decrease.amount;
			this.blockout_time_frames = decrease.blockout_time_frames;
			this.max_number_of_decreases = decrease.max_number_of_decreases;
			this.decrease_frequency = decrease.decrease_frequency;
			this.decrease_frequency_custom_minutes = decrease.decrease_frequency_custom_minutes;
		}

		#region abstract methods

		protected override bool ThresholdMet(long consumed_capacity_units, long current_capacity_units)
		{
			if (this.threshold.measurement_type == MeasurementTypes.Units)
			{
				long threshold = current_capacity_units - this.threshold.measurement_units;
				return consumed_capacity_units < threshold;
			}
			else // if (this.threshold_measurement_type == MeasurementTypes.Percentage)
			{
				byte consumed_capacity_percentage = (byte)(Math.Ceiling((((double)consumed_capacity_units / (double)current_capacity_units) * 100)));
				return consumed_capacity_percentage < (100 - this.threshold.measurement_percentage);
			}
		}

		protected override bool IsBlockedOut(ProvisionedThroughputDescription current_provisioned_throughput)
		{
			bool is_blocked_out = false;
			DateTime utc_now = DateTime.UtcNow;

			is_blocked_out = current_provisioned_throughput.NumberOfDecreasesToday == max_number_of_decreases;
			if (!is_blocked_out)
				is_blocked_out = this.blockout_time_frames.Any(blockout_time_frame => utc_now >= blockout_time_frame.Item1 && utc_now < blockout_time_frame.Item2);
			if (!is_blocked_out)
			{
				DateTime utc_today = utc_now.Date;
				DateTime last_decrease_date_time = current_provisioned_throughput.LastDecreaseDateTime;
				if (last_decrease_date_time < utc_today)
					last_decrease_date_time = utc_today;

				if (decrease_frequency == DecreaseFrequencies.EvenSpread)
				{
					DateTime utc_tomorrow = utc_today.AddDays(1);
					double total_remaining_minutes = (utc_tomorrow - last_decrease_date_time).TotalMinutes;
					List<Tuple<DateTime, DateTime>> remaining_blockout_time_frames = this.blockout_time_frames.Where(blockout_time_frame => last_decrease_date_time < blockout_time_frame.Item2).ToList();
					double total_blocked_out_minutes = remaining_blockout_time_frames.Sum(remaining_blockout_time_frame =>
					{
						if (last_decrease_date_time >= remaining_blockout_time_frame.Item1)
							return (remaining_blockout_time_frame.Item2 - last_decrease_date_time).TotalMinutes;
						else // if (last_decrease_date_time < blockout_time_frame.Item1)
							return (remaining_blockout_time_frame.Item2 - remaining_blockout_time_frame.Item1).TotalMinutes;
					});
					double remaining_minutes = total_remaining_minutes - total_blocked_out_minutes;
					double next_eligible_run_time_minutes = remaining_minutes / ((max_number_of_decreases - current_provisioned_throughput.NumberOfDecreasesToday) + 1);

					DateTime next_eligible_run_time = last_decrease_date_time;
					for (int index = 0; index < remaining_blockout_time_frames.Count && next_eligible_run_time_minutes > 0; index++)
					{
						Tuple<DateTime, DateTime> remaining_blockout_time_frame = remaining_blockout_time_frames.ElementAt(index);
						DateTime current_start = remaining_blockout_time_frame.Item1;
						DateTime current_end = remaining_blockout_time_frame.Item2;

						if (next_eligible_run_time >= current_start) // if last decrease date is within a blockout timeframe, then move it to the end of blockout timeframe in question
							next_eligible_run_time = current_end;
						else // if (next_eligible_run_time < current_start)
						{
							// calculates the minutes available until the next blockout timeframe
							double minutes_until_blockout_time_frame = (current_start - next_eligible_run_time).TotalMinutes;

							if (next_eligible_run_time_minutes < minutes_until_blockout_time_frame) // if next blockout timeframe is later than minutes until next eligible run time
							{
								next_eligible_run_time = next_eligible_run_time.AddMinutes(next_eligible_run_time_minutes);
								next_eligible_run_time_minutes = 0;
							}
							else if (next_eligible_run_time_minutes > minutes_until_blockout_time_frame)
							{
								next_eligible_run_time = current_end;
								next_eligible_run_time_minutes -= minutes_until_blockout_time_frame;
							}
							else // if (next_eligible_run_time_minutes == minutes_until_blockout_time_frame)
							{
								next_eligible_run_time = current_end;
								next_eligible_run_time_minutes = 0;
							}
						}
					}
					if (next_eligible_run_time_minutes > 0)
						next_eligible_run_time = next_eligible_run_time.AddMinutes(next_eligible_run_time_minutes);

					is_blocked_out = utc_now < next_eligible_run_time;
				}
				else if (decrease_frequency == DecreaseFrequencies.Custom)
				{
					DateTime next_eligible_run_time = last_decrease_date_time.AddMinutes(decrease_frequency_custom_minutes);
					is_blocked_out = utc_now < next_eligible_run_time;
				}
				//else if (decrease_frequency == DecreaseFrequencies.Immediate)
				//	skipped
			}

			return is_blocked_out;
		}

		protected override long GetNewCapacityUnits(long current_capacity_units)
		{
			if (this.amount.measurement_type == MeasurementTypes.Units)
				return current_capacity_units - this.amount.measurement_units;
			else // if (this.amount_measurement_type == MeasurementTypes.Percentage)
				return current_capacity_units - (long)Math.Ceiling(current_capacity_units * ((float)this.amount.measurement_percentage / 100));
		}

		#endregion
	}
}