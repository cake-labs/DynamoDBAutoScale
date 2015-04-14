using System.Collections.Generic;
using DynamoDBAutoScale.ConfigMapping;
using System.Linq;

namespace DynamoDBAutoScale
{
	public class ExcludesIncludes
	{
		public List<string> tables { get; set; }
		public List<string> indexes { get; set; }

		public ExcludesIncludes(ConfigMapping.ExcludesIncludes excludes_includes)
		{
			this.tables = new List<string>();
			this.indexes = new List<string>();

			if (excludes_includes != null)
			{
				if (excludes_includes.tables != null)
					this.tables = excludes_includes.tables.Select(table => table.Trim()).ToList();
				if (excludes_includes.indexes != null)
					this.indexes = excludes_includes.indexes.Select(index => index.Trim()).ToList();
			}
		}

		public bool MatchesTable(string table_name)
		{
			return tables.Any(table => Matches(table_name, table));
		}

		public bool MatchesIndexTable(string table_name)
		{
			return indexes.Any(index => Matches(table_name, index.Split('/')[0]));
		}

		public bool MatchesIndex(string table_name, string index_name)
		{
			return indexes.Any(index => Matches(table_name + "/" + index_name, index));
		}

		public int GetTableMatchScore(string table_name)
		{
			return tables.Where(table => Matches(table_name, table)).Max(table => table.Replace("*", string.Empty).Length);
		}

		public int GetIndexMatchScore(string table_name, string index_name)
		{
			return indexes.Where(index => Matches(table_name + "/" + index_name, index)).Max(index => index.Replace("*", string.Empty).Length);
		}

		private bool Matches(string name, string pattern)
		{
			string[] parts = pattern.Split('*');

			if (parts.Length == 1)
				return name == parts[0];
			else if (parts.Length == 2)
			{
				string part_1 = parts[0];
				string part_2 = parts[1];

				return (
					(part_1 == string.Empty && part_2 == string.Empty)
					|| (part_1 == string.Empty && name.EndsWith(part_2))
					|| (part_2 == string.Empty && name.StartsWith(part_1))
					|| (name.StartsWith(part_1) && name.Substring(part_1.Length).EndsWith(part_2))
				);
			}
			else
			{
				for (int index = 0; index < parts.Length; index++)
				{
					string part = parts[index];
					if (index == 0) // first part of pattern
					{
						if (part != string.Empty)
						{
							if (name.StartsWith(part))
								name = name.Substring(part.Length);
							else
								return false;
						}
					}
					else if (index == parts.Length - 1) // last part of pattern
					{
						if (part == string.Empty) // ends with *
							return true;
						else
							return name.EndsWith(part);
					}
					else
					{
						int part_index = name.IndexOf(part);
						if (part_index == -1)
							return false;
						else
							name = name.Substring(part_index + part.Length);
					}
				}
			}

			return name.Length == 0;
		}
	}
}