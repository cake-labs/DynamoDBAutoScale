using System.Collections.Generic;
using System.Xml.Serialization;

namespace DynamoDBAutoScale.ConfigMapping
{
	public class IncreaseDecrease
	{
		[XmlElement("Threshold")]
		public string threshold { get; set; }

		[XmlElement("Amount")]
		public string amount { get; set; }

		[XmlArray("BlockoutTimeFrames")]
		[XmlArrayItem("BlockoutTimeFrame")]
		public List<BlockoutTimeFrame> blockout_time_frames { get; set; }
	}
}