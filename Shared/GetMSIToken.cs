using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LogicAppAdvancedTool
{
    internal static class MSITokenService
    {
        public static MSIToken RetrieveToken(string resource)
        {
            string url = AppSettings.MSIEndpoint;
            string secret = AppSettings.MSISecret;

            Dictionary<string, string> headers = new Dictionary<string, string>()
            {
                { "X-IDENTITY-HEADER", secret }
            };

            string tokenUri = $"{url}?resource={resource}&api-version=2019-08-01";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(tokenUri);
            request.Method = "GET";
            request.Headers.Clear();

            foreach (KeyValuePair<string, string> header in headers)
            { 
                request.Headers.Add(header.Key, header.Value);
            }

            string result;

            using (HttpWebResponse wr = (HttpWebResponse)request.GetResponse())
            {
                using (StreamReader sr = new StreamReader(wr.GetResponseStream()))
                {
                    result = sr.ReadToEnd();

                    if (wr.StatusCode != HttpStatusCode.OK)
                    {
                        throw new ExpectedException($"Failed to retrieve token:\r\n{result}");
                    }
                }
            }

            MSIToken token = JsonConvert.DeserializeObject<MSIToken>(result);

            return token;
        }

        public class MSIToken
        { 
            public string access_token { get; set; }
            public string expires_on { get; set; }
            public string resource { get; set; }
            public string token_type { get; set; }
            public string client_id { get; set; }

            public MSIToken() { }
        }
    }
}
