using System.Collections.Generic;
using System.Xml.Serialization;

namespace DynamoDBAutoScale.ConfigMapping
{
	public class ExcludesIncludes
	{
		[XmlArray("Tables")]
		[XmlArrayItem("Table")]
		public List<string> tables { get; set; }

		[XmlArray("Indexes")]
		[XmlArrayItem("Index")]
		public List<string> indexes { get; set; }
	}
}