
namespace DynamoDBAutoScale
{
	public class ReadBasicAlarm : ReadWriteBasicAlarm
	{
		public ReadBasicAlarm(ConfigMapping.ReadWriteBasicAlarm read_write_basic_alarm) : base(read_write_basic_alarm) { }

		#region abstract methods

		protected override string GetAlarmName(string table_name)
		{
			return string.Format("{0}-ReadCapacityUnitsLimit-BasicAlarm", table_name);
		}

		protected override string GetMetricName()
		{
			return "ConsumedReadCapacityUnits";
		}

		#endregion
	}
}