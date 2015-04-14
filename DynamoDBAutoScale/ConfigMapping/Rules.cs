using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace DynamoDBAutoScale.ConfigMapping
{
	[XmlRoot("Rules")]
	public class Rules
	{
		[XmlElement("Rule")]
		public List<Rule> rules { get; set; }

		public List<ThroughputModification> GenerateThroughputModifications()
		{
			List<ThroughputModification> throghput_modifications = new List<ThroughputModification>();

			if (rules != null && rules.Any())
			{
				throghput_modifications = rules.Select((rule, index_id) =>
				{
					ThroughputModification throughput_modification = new ThroughputModification(index_id, rule);
					return throughput_modification;
				}).ToList();
			}

			return throghput_modifications;
		}
	}
}