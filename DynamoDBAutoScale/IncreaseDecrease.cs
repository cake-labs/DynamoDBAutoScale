using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Amazon.DynamoDBv2.Model;
using DynamoDBAutoScale.Enumerations;
using DynamoDBAutoScale.Results;

namespace DynamoDBAutoScale
{
	public abstract class IncreaseDecrease
	{
		public Measurement threshold { get; set; }
		public Measurement amount { get; set; }
		public List<Tuple<DateTime, DateTime>> blockout_time_frames { get; set; }

		public IncreaseDecrease() { }

		public IncreaseDecrease(ConfigMapping.IncreaseDecrease increase_decrease)
		{
			this.threshold = new Measurement(increase_decrease.threshold);
			this.amount = new Measurement(increase_decrease.amount);

			this.blockout_time_frames = new List<Tuple<DateTime, DateTime>>();
			if (increase_decrease.blockout_time_frames != null)
			{
				DateTime utc_now = DateTime.UtcNow;
				Func<string, DateTime?> blockout_time_frame_to_date_time = ((start_end) =>
				{
					DateTime? converted_date_time = null;
					
					if (!string.IsNullOrWhiteSpace(start_end))
					{
						try
						{
							string[] date_time_parts = start_end.Split(':');
							if (date_time_parts.Length == 2)
							{
								int hours = int.Parse(date_time_parts[0]);
								int minutes = int.Parse(date_time_parts[1]);

								if ((hours >= 0 && hours < 24) && (minutes >= 0 && minutes < 60))
									converted_date_time = new DateTime(utc_now.Year, utc_now.Month, utc_now.Day, hours, minutes, 0);
							}
						}
						catch (Exception exception)
						{
							// exception suppressed by default
							// provide custom exception handling here
						}
					}

					return converted_date_time;
				});

				List<Tuple<DateTime, DateTime>> listed_blockout_time_frames = new List<Tuple<DateTime, DateTime>>();
				increase_decrease.blockout_time_frames.ForEach(blockout_time_frame =>
				{
					DateTime? start = blockout_time_frame_to_date_time(blockout_time_frame.start);
					DateTime? end = blockout_time_frame_to_date_time(blockout_time_frame.end);
					if (start.HasValue && end.HasValue)
						listed_blockout_time_frames.Add(new Tuple<DateTime, DateTime>(start.Value, end.Value));
				});

				// combine overlapping time frames
				listed_blockout_time_frames = listed_blockout_time_frames.OrderBy(blockout_time_frame => blockout_time_frame.Item1).ThenBy(blockout_time_frame => blockout_time_frame.Item2).ToList();
				DateTime previous_start, previous_end;
				previous_start = previous_end = new DateTime();
				for (int index = 0; index < listed_blockout_time_frames.Count; index++)
				{
					Tuple<DateTime, DateTime> listed_blockout_time_frame = listed_blockout_time_frames.ElementAt(index);
					DateTime current_start = listed_blockout_time_frame.Item1;
					DateTime current_end = listed_blockout_time_frame.Item2;

					if (index == 0)
					{
						previous_start = current_start;
						previous_end = current_end;
					}
					else
					{
						if (current_start <= previous_end && current_end > previous_end)
							previous_end = current_end;
						else if (current_start > previous_end)
						{
							this.blockout_time_frames.Add(new Tuple<DateTime, DateTime>(previous_start, previous_end));

							previous_start = current_start;
							previous_end = current_end;
						}
					}

					if (index == (listed_blockout_time_frames.Count - 1))
						this.blockout_time_frames.Add(new Tuple<DateTime, DateTime>(previous_start, previous_end));
				}
			}
		}

		public IncreaseDecreaseResult ApplyRule(long consumed_capacity_units, long current_capacity_units, ProvisionedThroughputDescription current_provisioned_throughput)
		{
			bool threshold_met = false;
			bool blocked_out = false;
			long? new_capacity_units = null;

			threshold_met = ThresholdMet(consumed_capacity_units, current_capacity_units);
			if (threshold_met)
			{
				blocked_out = IsBlockedOut(current_provisioned_throughput);
				if (!blocked_out)
					new_capacity_units = GetNewCapacityUnits(current_capacity_units);
			}

			IncreaseDecreaseResult increase_decrease_result = new IncreaseDecreaseResult(threshold, amount, threshold_met, blocked_out, new_capacity_units);
			return increase_decrease_result;
		}

		#region abstract methods

		abstract protected bool ThresholdMet(long consumed_capacity_units, long current_capacity_units);

		abstract protected bool IsBlockedOut(ProvisionedThroughputDescription current_provisioned_throughput);

		abstract protected long GetNewCapacityUnits(long current_capacity_units);

		#endregion
	}
}