using System;
using System.Configuration;
using DynamoDBAutoScale;
using DynamoDBAutoScale.Results;

namespace DynamoDBAutoScaleExample
{
	class Program
	{
		static void Main(string[] args)
		{
			// load aws settings from app.config
			//AWS.LoadSettings();

			// manually set AWS settings
			AWS.AccessKeyID = ConfigurationManager.AppSettings.Get("AWSAccessKeyID");
			AWS.SecretAccessKey = ConfigurationManager.AppSettings.Get("AWSSecretAccessKey");
			AWS.RegionEndpoint = ConfigurationManager.AppSettings.Get("AWSRegionEndpoint");

			// build modifications
			PreparedModifications prepared_modifications = AutoScale.BuildModificatons();

			// print out changes to be made
			Console.WriteLine(prepared_modifications.ToString(true));

			// apply modifications
			//ModificationResults modification_results = AutoScale.ApplyModifications(prepared_modifications);

			// print out modification results
			//Console.WriteLine(modification_results.ToString());

			Console.WriteLine("Done");
			Console.Read();
		}
	}
}