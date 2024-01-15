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
            string result = "{\"access_token\":\"eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjVCM25SeHRRN2ppOGVORGMzRnkwNUtmOTdaRSIsImtpZCI6IjVCM25SeHRRN2ppOGVORGMzRnkwNUtmOTdaRSJ9.eyJhdWQiOiJodHRwczovL21hbmFnZW1lbnQuYXp1cmUuY29tIiwiaXNzIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvNzJmOTg4YmYtODZmMS00MWFmLTkxYWItMmQ3Y2QwMTFkYjQ3LyIsImlhdCI6MTcwNTI5NjE1MCwibmJmIjoxNzA1Mjk2MTUwLCJleHAiOjE3MDUzODI4NTAsImFpbyI6IkUyVmdZQWl3bkdjaHRkRS9kUk9mOWUvcjdGK1BBd0E9IiwiYXBwaWQiOiIzODVkN2NmYi0zMWVjLTQyMDYtODk1NC04Y2NmZWIzYzE2ZmUiLCJhcHBpZGFjciI6IjIiLCJpZHAiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC83MmY5ODhiZi04NmYxLTQxYWYtOTFhYi0yZDdjZDAxMWRiNDcvIiwiaWR0eXAiOiJhcHAiLCJvaWQiOiIzYzI0OTU5ZS05ZTFmLTQ4YzItYjQ2OC00ZmM5ZmIzMDZjNTkiLCJyaCI6IjAuQVJvQXY0ajVjdkdHcjBHUnF5MTgwQkhiUjBaSWYza0F1dGRQdWtQYXdmajJNQk1hQUFBLiIsInN1YiI6IjNjMjQ5NTllLTllMWYtNDhjMi1iNDY4LTRmYzlmYjMwNmM1OSIsInRpZCI6IjcyZjk4OGJmLTg2ZjEtNDFhZi05MWFiLTJkN2NkMDExZGI0NyIsInV0aSI6Im5WQWx2MXhKY2tPVnNqY0VITm5aQUEiLCJ2ZXIiOiIxLjAiLCJ4bXNfY2FlIjoiMSIsInhtc19taXJpZCI6Ii9zdWJzY3JpcHRpb25zL2FhZmIyN2RlLWU3MjgtNGNiOC05ZTY4LTlhZmRlMWQxYTg0OS9yZXNvdXJjZWdyb3Vwcy9Mb2dpY0FwcFN0YW5kYXJkL3Byb3ZpZGVycy9NaWNyb3NvZnQuV2ViL3NpdGVzL0RyYWNMb2dpY0FwcCIsInhtc190Y2R0IjoiMTI4OTI0MTU0NyJ9.sG3-Gwvpmm_ngcVURMHKZlmFILgyhlzLsT3TGoOrX5YmCCPLdQHXrUFsnQJNGAlde9HpWgAj7xPZQZK7fvLwyGCVMpWfU9yh2ApA_gh-xRg9wBSyX1iuoOyQC6z6i167chVOpCnjyuWzWgceEkGAF3uPOq58nSs5iKRpVyyHEnpEHzCuKneviFobN1jC8xESgUv97Tf4vKMaIAhS2QnRHr14kwpNhfOh50Ja249DsBAGoId4pr9cwH5NAYdE9zzA-_WqOGaN9m9qy0WDoNAJOkRHoOuYN8C076jjDdhoHtsw-xTuHZHkUkni4mhnMgkvDBGDI3cZrol6VMct8saQwQ\",\"expires_on\":\"1694141276\",\"resource\":\"https://management.azure.com\",\"token_type\":\"Bearer\",\"client_id\":\"5D1FE1E8-B908-419B-9BAA-6D9BAEA11D75\"}";
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
