using Amazon.DynamoDBv2.Model;
using DynamoDBAutoScale.Enumerations;

namespace DynamoDBAutoScale
{
	public class Read : ReadWrite
	{
		public Read(ConfigMapping.ReadWrite read_write, DecreaseFrequencies decrease_frequency, int decrease_frequency_custom_minutes) : base(read_write, decrease_frequency, decrease_frequency_custom_minutes)
		{
			this.basic_alarm = (read_write.basic_alarm != null ? new ReadBasicAlarm(read_write.basic_alarm) : null);
		} 

		#region abstract methods

		protected override long GetConsumedCapacityUnits(string table_name, string index_name, int look_back_minutes)
		{
			return this.GetConsumedCapacityUnits(table_name, index_name, "ConsumedReadCapacityUnits", look_back_minutes);
		}

		protected override long GetCurrentCapacityUnits(ProvisionedThroughputDescription current_provisioned_throughput)
		{
			return current_provisioned_throughput.ReadCapacityUnits;
		}

		#endregion
	}
}