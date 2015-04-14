using System;
using System.Configuration;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace DynamoDBAutoScale.ConfigMapping
{
	public class XMLSerializerSectionHandler : IConfigurationSectionHandler
	{
		public object Create(object parent, object config_context, XmlNode section)
		{
			XPathNavigator xpath_navigation = section.CreateNavigator();
			string type_name = (string)xpath_navigation.Evaluate("string(@type)");
			Type type = Type.GetType(type_name);
			XmlSerializer xml_serializer = new XmlSerializer(type);
			return xml_serializer.Deserialize(new XmlNodeReader(section));
		}
	}
}