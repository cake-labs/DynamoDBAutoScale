using System.Collections.Generic;
using System.Xml.Serialization;

namespace DynamoDBAutoScale.ConfigMapping
{
	public class Rule
	{
		[XmlElement("LookBackMinutes")]
		public int? look_back_minutes { get; set; }

		[XmlElement("DecreaseFrequency")]
		public string decrease_frequency { get; set; }

		[XmlElement("Excludes")]
		public ExcludesIncludes excludes { get; set; }

		[XmlElement("Includes")]
		public ExcludesIncludes includes { get; set; }

		[XmlElement("Read")]
		public ReadWrite read { get; set; }

		[XmlElement("Write")]
		public ReadWrite write { get; set; }
	}
}