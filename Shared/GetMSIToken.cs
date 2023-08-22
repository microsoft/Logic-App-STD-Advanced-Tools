using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        public static void GetMSIToken()
        {
            string url = AppSettings.MSIEndpoint;
            string secret = AppSettings.MSISecret;

            Dictionary<string, string> headers = new Dictionary<string, string>()
            {
                { "X-IDENTITY-HEADER", secret }
            };

            string tokenUri = $"{url}?resource=https://storage.azure.com&api-version=2019-08-01";

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Clear();

            foreach (KeyValuePair<string, string> header in headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            HttpResponseMessage response = client.GetAsync(tokenUri).GetAwaiter().GetResult();

            string responseContent = response.Content.ReadAsStringAsync().Result;

            Console.WriteLine(response.StatusCode);
            Console.WriteLine(responseContent);

            return;
        }
    }
}
