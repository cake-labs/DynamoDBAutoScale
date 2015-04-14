using System.Xml.Serialization;

namespace DynamoDBAutoScale.ConfigMapping
{
	public class BlockoutTimeFrame
	{
		[XmlElement("Start")]
		public string start { get; set; }

		[XmlElement("End")]
		public string end { get; set; }
	}
}