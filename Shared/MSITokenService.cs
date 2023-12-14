using LogicAppAdvancedTool.Structures;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;

namespace LogicAppAdvancedTool
{
    public static class MSITokenService
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
#if !DEBUG
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Clear();
            foreach (KeyValuePair<string, string> header in headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            MSIToken token = client.GetFromJsonAsync<MSIToken>(tokenUri).Result;
#else
            //for local debugging, during development, we cannot get MI of LA, so we have to generate MI token on Kudu and provide here as hardcode (via using Tools GetMIToken command).
            //MI token expiry every day, so no security issue to leave it in code
            Console.WriteLine("DEBUG mode, use pre-generated token.");
            string result = "{\"access_token\":\"eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IlQxU3QtZExUdnlXUmd4Ql82NzZ1OGtyWFMtSSIsImtpZCI6IlQxU3QtZExUdnlXUmd4Ql82NzZ1OGtyWFMtSSJ9.eyJhdWQiOiJodHRwczovL21hbmFnZW1lbnQuYXp1cmUuY29tIiwiaXNzIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvNzJmOTg4YmYtODZmMS00MWFmLTkxYWItMmQ3Y2QwMTFkYjQ3LyIsImlhdCI6MTcwMjQ1MDM0OSwibmJmIjoxNzAyNDUwMzQ5LCJleHAiOjE3MDI1MzcwNDksImFpbyI6IkUyVmdZTkM0dU1lNnJQYlRRL2RUMTIwOVd6WWtBZ0E9IiwiYXBwaWQiOiIzODVkN2NmYi0zMWVjLTQyMDYtODk1NC04Y2NmZWIzYzE2ZmUiLCJhcHBpZGFjciI6IjIiLCJpZHAiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC83MmY5ODhiZi04NmYxLTQxYWYtOTFhYi0yZDdjZDAxMWRiNDcvIiwiaWR0eXAiOiJhcHAiLCJvaWQiOiIzYzI0OTU5ZS05ZTFmLTQ4YzItYjQ2OC00ZmM5ZmIzMDZjNTkiLCJyaCI6IjAuQVJvQXY0ajVjdkdHcjBHUnF5MTgwQkhiUjBaSWYza0F1dGRQdWtQYXdmajJNQk1hQUFBLiIsInN1YiI6IjNjMjQ5NTllLTllMWYtNDhjMi1iNDY4LTRmYzlmYjMwNmM1OSIsInRpZCI6IjcyZjk4OGJmLTg2ZjEtNDFhZi05MWFiLTJkN2NkMDExZGI0NyIsInV0aSI6Im1sYzRVN1VMdEVtTWl6YkN2Y0ZWQUEiLCJ2ZXIiOiIxLjAiLCJ4bXNfY2FlIjoiMSIsInhtc19taXJpZCI6Ii9zdWJzY3JpcHRpb25zL2FhZmIyN2RlLWU3MjgtNGNiOC05ZTY4LTlhZmRlMWQxYTg0OS9yZXNvdXJjZWdyb3Vwcy9Mb2dpY0FwcFN0YW5kYXJkL3Byb3ZpZGVycy9NaWNyb3NvZnQuV2ViL3NpdGVzL0RyYWNMb2dpY0FwcCIsInhtc190Y2R0IjoiMTI4OTI0MTU0NyJ9.cp5yddABgAZjxDpVtlPK_EndgiNIhlOGr9lse9cZlaio07nk7grjuhIxMFmi-GuedOvj39aSEU78GPDdQ6CO2sekXxmiB53VQQyebehNBlAXw8SJOMIrG464t8k2OL4ADWBAul51LjiRs2MAGlnTN075BYgCCJUogsKwb4gB2zXSAgzYIkPb5YZsXj9hJ4IDj_yZCR7VUvCRUyoqpl1xNkpCmr7F0gwP08J37169dWPQnB6BbJfmTk-xztiNz59XH6Ma1fD6JSQ5BnuocJkDssPh1Iow3QMZCXQs-6aFZeJK7SKbbt7xSkfaXxu3ZP-cOmzQbW8ToXC1iGliGRi06g\",\"expires_on\":\"1694141276\",\"resource\":\"https://management.azure.com\",\"token_type\":\"Bearer\",\"client_id\":\"5D1FE1E8-B908-419B-9BAA-6D9BAEA11D75\"}";
            MSIToken token = JsonConvert.DeserializeObject<MSIToken>(result);
#endif
            return token;
        }

        public static void VerifyToken(ref MSIToken token)
        {
            //When in debug mode (run in local), it is using pre-generated token and not necessary to check token expired or not
#if !DEBUG    
            long epochNow = DateTime.UtcNow.ToEpoch();
            long diff = long.Parse(token.expires_on) - epochNow;

            if (diff < 300)
            {
                Console.WriteLine($"MSI token will be expired in {diff} seconds, refresh token.");

                token = RetrieveToken("https://management.azure.com");
            }
#endif
        }
    }
}
