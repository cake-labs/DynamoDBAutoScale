using System.Collections.Generic;
using System.Xml.Serialization;

namespace DynamoDBAutoScale.ConfigMapping
{
	public class ReadWriteBasicAlarm
	{
		[XmlElement("Threshold")]
		public string threshold { get; set; }

		[XmlElement("PeriodMinutes")]
		public int? period_minutes { get; set; }

		[XmlElement("ConsecutivePeriods")]
		public int? consecutive_periods { get; set; }

		[XmlArray("Actions")]
		[XmlArrayItem("Action")]
		public List<string> actions { get; set; }
	}
}