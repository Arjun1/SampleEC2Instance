using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Xml;
using System.IO;

namespace AWS.SDK.EC2
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			// #1 describe the instance
			string timestamp = CalculateTimestamp();

			//create string to sign  must be alpha ordered.
			string stringToConvert = "GET\n" +
				"ec2.amazonaws.com\n" +
				"/\n" +
				"AWSAccessKeyId=AKIAI2POF6TWGLEHB7UQ" +
				"&Action=DescribeInstances" +
				//"&Filter.1.Name=availability-zone" +
				//"&Filter.1.value.1=us.east-1a" +
				"&SignatureMethod=HmacSHA1" +
				"&SignatureVersion=2" +
				"&Timestamp=" + timestamp +
				"&Version=2011-12-15"; // IMM uses version

			string awsPrivateKey = "1sqdFU0wo8kVyQTBmsu42NMmfirOSqhgireRMGw8";

			Encoding ae = new UTF8Encoding();
			HMACSHA1 signature = new HMACSHA1();
			signature.Key = ae.GetBytes(awsPrivateKey);
			byte[] bytes = ae.GetBytes(stringToConvert);
			byte[] moreBytes = signature.ComputeHash(bytes);
			string encodedCanonical = Convert.ToBase64String(moreBytes);
			string urlEncodedCanonical = HttpUtility.UrlEncode(encodedCanonical).Replace("+","%20").Replace("%3d","%3D").Replace("%3a","%3A");

			//actual URL string
			string ec2Url = "https://ec2.amazonaws.com/?Action=DescribeInstances" +
				"&Version=2011-12-15" +
				"&Timestamp=" + timestamp +
				"&Signature=" + urlEncodedCanonical +
				"&SignatureVersion=2" +
				"&SignatureMethod=HmacSHA1" +
				"&AWSAccessKeyId=AKIAI2POF6TWGLEHB7UQ";

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
