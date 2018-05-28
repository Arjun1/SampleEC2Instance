using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Xml;
using System.IO;
using Amazon.EC2;
using Amazon.EC2.Model;
using System.Configuration;

namespace AWS.SDK.EC2
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			// query using .net native api
			queryForInstancesDescription();

			// add tag to an existing instance using Amazon SDK lib
			AddTag();
		}

		private static void AddTag()
		{
			AmazonEC2Client client = new AmazonEC2Client();
			CreateTagsRequest tagReq = new CreateTagsRequest();
			tagReq.Resources = new System.Collections.Generic.List<string> { "instanceId" };
			tagReq.Tags = new System.Collections.Generic.List<Tag> { new Tag() { Key = "tagKey", Value = "tagValue" } };
			CreateTagsResponse tagResp = client.CreateTags(tagReq);
			Console.WriteLine("EC2 Tag created- {0}", tagResp.ResponseMetadata);
		}

		private static void queryForInstancesDescription()
		{
			// #1 describe the instance
			string timestamp = CalculateTimestamp();
			string accessKey = ConfigurationManager.AppSettings.Get("AWSAccessKey");
			string awsPrivateKey = ConfigurationManager.AppSettings.Get("AWSSecreteKey");
			//create string to sign  must be alpha ordered.
			string stringToConvert = "GET\n" +
				"ec2.amazonaws.com\n" +
				"/\n" +
				"AWSAccessKeyId=" + accessKey +
				"&Action=DescribeInstances" +
				//"&Filter.1.Name=availability-zone" +
				//"&Filter.1.value.1=us.east-1a" +
				"&SignatureMethod=HmacSHA1" +
				"&SignatureVersion=2" +
				"&Timestamp=" + timestamp +
				"&Version=2011-12-15"; // IAM uses version


			Encoding ae = new UTF8Encoding();
			HMACSHA1 signature = new HMACSHA1();
			signature.Key = ae.GetBytes(awsPrivateKey);
			byte[] bytes = ae.GetBytes(stringToConvert);
			byte[] moreBytes = signature.ComputeHash(bytes);
			string encodedCanonical = Convert.ToBase64String(moreBytes);
			string urlEncodedCanonical = HttpUtility.UrlEncode(encodedCanonical)
			                                        .Replace("+", "%20")
			                                        .Replace("%3d", "%3D")
			                                        .Replace("%3a", "%3A");

			//actual URL string
			string ec2Url = "https://ec2.amazonaws.com/?Action=DescribeInstances" +
				"&Version=2011-12-15" +
				"&Timestamp=" + timestamp +
				"&Signature=" + urlEncodedCanonical +
				"&SignatureVersion=2" +
				"&SignatureMethod=HmacSHA1" +
				"&AWSAccessKeyId={}";

			HttpWebRequest req = WebRequest.Create(ec2Url) as HttpWebRequest;
			XmlDocument xmlDoc = new XmlDocument();
			using (HttpWebResponse resp = req.GetResponse() as HttpWebResponse)
			{
				StreamReader reader = new StreamReader(resp.GetResponseStream());
				string responseXml = reader.ReadToEnd();
				xmlDoc.LoadXml(responseXml);
			}
			Console.WriteLine("EC2 instances queried...");
			Console.WriteLine("[AWS EC2 RESPONSE]: {0}", xmlDoc.OuterXml);
		}

		private static string CalculateTimestamp()
		{
			string timestamp = Uri.EscapeUriString(string.Format("{0:s}", DateTime.UtcNow)).ToString();
			timestamp = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
			timestamp = HttpUtility.UrlEncode(timestamp).Replace("%3a", "%3A");
			return timestamp;
		}
	}
}
