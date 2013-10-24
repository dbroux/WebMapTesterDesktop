using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebMapTester
{
	public class PortalInfo
	{
		public PortalInfo(string url, string description = null, string oAuthClientId = null)
		{
			Url = url;
			Description = description ?? Regex.Replace(url, "sharing/.*", "");
			OAuthClientId = oAuthClientId;
		}

		public string Description { get; private set; }
		public string Url { get; private set; }
		public string OAuthClientId { get; private set; }
	}
}
