using System.Configuration;
using System.IO;
using System.Net;
using Amazon;
using Amazon.CloudWatch;
using Amazon.DynamoDBv2;

namespace DynamoDBAutoScale
{
	public static class AWS
	{
		private static string access_key_id { get; set; }
		private static string secret_access_key { get; set; }
		private static string region_endpoint { get; set; }

		public static string AccessKeyID
		{
			get { return access_key_id; }
			set { access_key_id = value; }
		}

		public static string SecretAccessKey
		{
			get { return secret_access_key; }
			set { secret_access_key = value; }
		}

		public static string RegionEndpoint
		{
			get { return region_endpoint; }
			set { region_endpoint = value; }
		}

		private static string GetCurrentPlacement()
		{
			string current_placement = null;

			WebRequest request = HttpWebRequest.Create("http://169.254.169.254/latest/meta-data/placement/availability-zone/");
			using (WebResponse response = request.GetResponse())
			{
				using (Stream response_stream = response.GetResponseStream())
				{
					using (StreamReader stream_reader = new StreamReader(response_stream))
					{
						current_placement = stream_reader.ReadToEnd();
					}
				}
			}

			return current_placement;
		}

		private static Amazon.RegionEndpoint GetRegion()
		{
			if (string.IsNullOrEmpty(region_endpoint))
				region_endpoint = GetCurrentPlacement();
			return Amazon.RegionEndpoint.GetBySystemName(region_endpoint);
		}

		public static AmazonDynamoDBClient GetAmazonDynamoDBClient()
		{
			AmazonDynamoDBClient amazon_dynamo_db_client = (!string.IsNullOrEmpty(access_key_id) && !string.IsNullOrEmpty(secret_access_key)
				? new AmazonDynamoDBClient(access_key_id, secret_access_key, GetRegion())
				: new AmazonDynamoDBClient(GetRegion()));
			return amazon_dynamo_db_client;
		}

		public static AmazonCloudWatchClient GetAmazonCloudWatchClient()
		{
			AmazonCloudWatchClient amazon_cloud_watch_client = (!string.IsNullOrEmpty(access_key_id) && !string.IsNullOrEmpty(secret_access_key)
				? new AmazonCloudWatchClient(access_key_id, secret_access_key, GetRegion())
				: new AmazonCloudWatchClient(GetRegion()));
			return amazon_cloud_watch_client;
		}

		public static void LoadSettings()
		{
			access_key_id = ConfigurationManager.AppSettings.Get("AWSAccessKeyID");
			secret_access_key = ConfigurationManager.AppSettings.Get("AWSSecretAccessKey");
			region_endpoint = ConfigurationManager.AppSettings.Get("AWSRegionEndpoint");
		}
	}
}