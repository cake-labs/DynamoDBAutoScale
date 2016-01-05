using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.CloudWatch.Model;
using Amazon.CloudWatch;
using DynamoDBAutoScale.Enumerations;

namespace DynamoDBAutoScale
{
	public abstract class ReadWriteBasicAlarm
	{
		public Measurement threshold { get; set; }
		public int period_minutes { get; set; }
		public int consecutive_periods { get; set; }
		public List<string> actions { get; set; }

		private const int period_minutes_default = 5;
		private const int consecutive_periods_default = 2;

		public ReadWriteBasicAlarm(ConfigMapping.ReadWriteBasicAlarm read_write_basic_alarm)
		{
			this.threshold = new Measurement(read_write_basic_alarm.threshold);

			this.period_minutes = period_minutes_default;
			if (read_write_basic_alarm.period_minutes.HasValue)
				this.period_minutes = read_write_basic_alarm.period_minutes.Value;

			this.consecutive_periods = consecutive_periods_default;
			if (read_write_basic_alarm.consecutive_periods.HasValue)
				this.consecutive_periods = read_write_basic_alarm.consecutive_periods.Value;

			this.actions = new List<string>();
			if (read_write_basic_alarm.actions != null)
				this.actions = read_write_basic_alarm.actions.Take(5).ToList();
		}

		private long GetAverageUnitsPerSecond(long provisioned_capacity_units)
		{
			if (this.threshold.measurement_type == MeasurementTypes.Units)
				return provisioned_capacity_units - this.threshold.measurement_units;
			else // if (this.threshold.measurement_type == MeasurementTypes.Percentage)
				return provisioned_capacity_units - (long)Math.Ceiling(provisioned_capacity_units * ((float)this.threshold.measurement_percentage / 100));
		}

		public void Enable(string table_name, long provisioned_capacity_units)
		{
			AmazonCloudWatchClient amazon_cloud_watch_client = AWS.GetAmazonCloudWatchClient();

			string alarm_name = GetAlarmName(table_name);
			string metric_name = GetMetricName();
			double average_units_per_second = GetAverageUnitsPerSecond(provisioned_capacity_units);
			int period_minutes_total_seconds = (period_minutes * 60);
			
			PutMetricAlarmRequest put_metric_alarm_request = new PutMetricAlarmRequest
			{
				AlarmName = alarm_name,
				Namespace = "AWS/DynamoDB",
				Dimensions = new List<Dimension>
				{
					new Dimension
					{
						Name = "TableName",
						Value = table_name
					}
				},
				MetricName = metric_name,
				Statistic = Statistic.Sum,
				ComparisonOperator = Amazon.CloudWatch.ComparisonOperator.GreaterThanOrEqualToThreshold,
				Threshold = average_units_per_second * period_minutes_total_seconds,
				Period = period_minutes_total_seconds,
				EvaluationPeriods = consecutive_periods,
				ActionsEnabled = true,
				AlarmActions = actions
			};

			PutMetricAlarmResponse put_metric_alarm_response = amazon_cloud_watch_client.PutMetricAlarm(put_metric_alarm_request);
		}

		#region abstract methods

		abstract protected string GetAlarmName(string table_name);

		abstract protected string GetMetricName();

		#endregion
	}
}