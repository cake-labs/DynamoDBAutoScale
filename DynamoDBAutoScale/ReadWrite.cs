using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.DynamoDBv2.Model;
using DynamoDBAutoScale.Enumerations;
using DynamoDBAutoScale.Results;

namespace DynamoDBAutoScale
{
	public abstract class ReadWrite
	{
		public long? maximum_throughput { get; set; }
		public long? minimum_throughput { get; set; }

		Measurement decrease_combination_modifier { get; set; }

		private long consumed_capacity_units { get; set; }
		private long current_capacity_units { get; set; }

		public Increase increase { get; set; }
		public Decrease decrease { get; set; }
		public ReadWriteBasicAlarm basic_alarm { get; set; }

		public ReadWrite(ConfigMapping.ReadWrite read_write, DecreaseFrequencies decrease_frequency, int decrease_frequency_custom_minutes)
		{
			this.maximum_throughput = read_write.maximum_throughput;
			this.minimum_throughput = read_write.minimum_throughput;
			this.increase = (read_write.increase != null ? new Increase(read_write.increase) : null);
			this.decrease = (read_write.decrease != null ? new Decrease(read_write.decrease, decrease_frequency, decrease_frequency_custom_minutes) : null);
			this.decrease_combination_modifier = new Measurement(read_write.decrease_combination_modifier);
		}

		protected long GetConsumedCapacityUnits(string table_name, string index_name, string metric_name, int look_back_minutes)
		{
			List<Dimension> dimensions = new List<Dimension> { new Dimension { Name = "TableName", Value = table_name } };
			if (!string.IsNullOrWhiteSpace(index_name))
				dimensions.Add(new Dimension { Name = "GlobalSecondaryIndexName", Value = index_name });

			DateTime end_time = DateTime.UtcNow.AddMinutes(-1);
			DateTime start_time = end_time.AddMinutes(-look_back_minutes);

			AmazonCloudWatchClient amazon_cloud_watch_client = AWS.GetAmazonCloudWatchClient();

			GetMetricStatisticsRequest get_metric_statistics_request = new GetMetricStatisticsRequest
			{
				Namespace = "AWS/DynamoDB",
				Dimensions = dimensions,
				MetricName = metric_name,
				StartTime = start_time,
				EndTime = end_time,
				Period = 60,
				Statistics = new List<string> { "Sum" }
			};

			GetMetricStatisticsResponse get_metric_statistics_response = amazon_cloud_watch_client.GetMetricStatistics(get_metric_statistics_request);

			long consumed_capacity_units = 0;
			if (get_metric_statistics_response != null && get_metric_statistics_response.Datapoints.Any())
				consumed_capacity_units = (long)(get_metric_statistics_response.Datapoints.Max(datapoint => datapoint.Sum) / 60);

			return consumed_capacity_units;
		}

		public ReadWriteResult ApplyRule(string table_name, string index_name, int look_back_minutes, ProvisionedThroughputDescription current_provisioned_throughput)
		{
			ReadWriteResult read_write_result = new ReadWriteResult();
			consumed_capacity_units = GetConsumedCapacityUnits(table_name, index_name, look_back_minutes);
			current_capacity_units = GetCurrentCapacityUnits(current_provisioned_throughput);

			read_write_result.consumed_capacity_units = consumed_capacity_units;
			read_write_result.look_back_minutes = look_back_minutes;

			// when provided poorly set up rules, favor increase over decrease for safety
			if (increase != null)
			{
				read_write_result.increase_result = this.increase.ApplyRule(consumed_capacity_units, current_capacity_units, current_provisioned_throughput);
				read_write_result.new_capacity_units = read_write_result.increase_result.new_capacity_units;
			}
			if (!read_write_result.new_capacity_units.HasValue && decrease != null)
			{
				read_write_result.decrease_result = this.decrease.ApplyRule(consumed_capacity_units, current_capacity_units, current_provisioned_throughput);
				read_write_result.new_capacity_units = read_write_result.decrease_result.new_capacity_units;
			}

			return read_write_result;
		}

		public IncreaseDecreaseResult GetNewDecreaseCombinationModifierCapacityUnits(ProvisionedThroughputDescription current_provisioned_throughput)
		{
			IncreaseDecreaseResult decrease_combination_modifier_result = null;

			if (decrease != null && (decrease_combination_modifier.measurement_units > 0 || decrease_combination_modifier.measurement_percentage > 0))
			{
				Decrease combination_decrease = new Decrease(decrease, decrease_combination_modifier);
				decrease_combination_modifier_result = combination_decrease.ApplyRule(consumed_capacity_units, current_capacity_units, current_provisioned_throughput);
			}

			return decrease_combination_modifier_result;
		}

		#region abstract methods

		abstract protected long GetConsumedCapacityUnits(string table_name, string index_name, int look_back_minutes);

		abstract protected long GetCurrentCapacityUnits(ProvisionedThroughputDescription current_provisioned_throughput);

		#endregion
	}
}