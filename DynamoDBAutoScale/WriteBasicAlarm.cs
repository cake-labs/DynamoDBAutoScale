
namespace DynamoDBAutoScale
{
	public class WriteBasicAlarm : ReadWriteBasicAlarm
	{
		public WriteBasicAlarm(ConfigMapping.ReadWriteBasicAlarm read_write_basic_alarm) : base(read_write_basic_alarm) { }

		#region abstract methods

		protected override string GetAlarmName(string table_name)
		{
			return string.Format("{0}-WriteCapacityUnitsLimit-BasicAlarm", table_name);
		}

		protected override string GetMetricName()
		{
			return "ConsumedWriteCapacityUnits";
		}

		#endregion
	}
}