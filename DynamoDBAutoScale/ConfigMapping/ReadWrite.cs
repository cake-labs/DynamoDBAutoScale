using System.Xml.Serialization;
using System.ComponentModel;

namespace DynamoDBAutoScale.ConfigMapping
{
	public class ReadWrite
	{
		[XmlElement("MaximumThroughput")]
		public long? maximum_throughput { get; set; }

		[XmlElement("MinimumThroughput")]
		public long? minimum_throughput { get; set; }

		[XmlElement("DecreaseCombinationModifier")]
		public string decrease_combination_modifier { get; set; }

		[XmlElement("Increase")]
		public IncreaseDecrease increase { get; set; }

		[XmlElement("Decrease")]
		public IncreaseDecrease decrease { get; set; }

		[XmlElement("BasicAlarm")]
		public ReadWriteBasicAlarm basic_alarm { get; set; }
	}
}