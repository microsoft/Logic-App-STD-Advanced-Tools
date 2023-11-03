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
            Console.WriteLine("DEBUG mode, use pre-generated token.");
            string result = "{\"access_token\":\"eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjlHbW55RlBraGMzaE91UjIybXZTdmduTG83WSIsImtpZCI6IjlHbW55RlBraGMzaE91UjIybXZTdmduTG83WSJ9.eyJhdWQiOiJodHRwczovL21hbmFnZW1lbnQuYXp1cmUuY29tIiwiaXNzIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvNzJmOTg4YmYtODZmMS00MWFmLTkxYWItMmQ3Y2QwMTFkYjQ3LyIsImlhdCI6MTY5ODk3NTEzNCwibmJmIjoxNjk4OTc1MTM0LCJleHAiOjE2OTkwNjE4MzQsImFpbyI6IkUyRmdZTGgwZW5yTUp4RnAyN2lsYldacldQWjBBZ0E9IiwiYXBwaWQiOiI1ZDFmZTFlOC1iOTA4LTQxOWItOWJhYS02ZDliYWVhMTFkNzUiLCJhcHBpZGFjciI6IjIiLCJpZHAiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC83MmY5ODhiZi04NmYxLTQxYWYtOTFhYi0yZDdjZDAxMWRiNDcvIiwiaWR0eXAiOiJhcHAiLCJvaWQiOiJmNjBlZGI1OC0yOTk3LTRjZTctOTE2NC1kM2MwNWQxZGY0ZWYiLCJyaCI6IjAuQUJvQXY0ajVjdkdHcjBHUnF5MTgwQkhiUjBaSWYza0F1dGRQdWtQYXdmajJNQk1hQUFBLiIsInN1YiI6ImY2MGVkYjU4LTI5OTctNGNlNy05MTY0LWQzYzA1ZDFkZjRlZiIsInRpZCI6IjcyZjk4OGJmLTg2ZjEtNDFhZi05MWFiLTJkN2NkMDExZGI0NyIsInV0aSI6Ik42b293bHNSd1VTYXo1dWhvblFrQUEiLCJ2ZXIiOiIxLjAiLCJ4bXNfY2FlIjoiMSIsInhtc19taXJpZCI6Ii9zdWJzY3JpcHRpb25zL2FhZmIyN2RlLWU3MjgtNGNiOC05ZTY4LTlhZmRlMWQxYTg0OS9yZXNvdXJjZWdyb3Vwcy9Mb2dpY0FwcFN0YW5kYXJkL3Byb3ZpZGVycy9NaWNyb3NvZnQuV2ViL3NpdGVzL0RyYWNMb2dpY0FwcCIsInhtc190Y2R0IjoxMjg5MjQxNTQ3fQ.TEeWuixnRYQUCOiuiV0Y9XmWVednjpbnOC0ArITCtr2wGKeyw0K3gFWf7kP04buuj3vCTuKidFgqkwNzwBJZ-8PaFUANqrHmoYaJ71X8Ws7YMU4wE2syXo7g1TJjU-RKEEaY0ILh7dgVmWNiwK7nJgvC6IkG3JUL9szQvmD6BlepBqI5MwzyRKEaBeftBbGlOjTgggekR2DZhxF6NX3hbDwvrseMPVbroVZptVTn6L65GKABwZln9UfKeoGhmiTPtu2MNHzDbsuyJFPyeC_pVYDZ3IlYc6eNhrqe59a-rT0m0FXOF1iZP8LUMPzRSFPoC5v5FpX1YQISKeMMlOeYyw\",\"expires_on\":\"1694141276\",\"resource\":\"https://management.azure.com\",\"token_type\":\"Bearer\",\"client_id\":\"5D1FE1E8-B908-419B-9BAA-6D9BAEA11D75\"}";
            MSIToken token = JsonConvert.DeserializeObject<MSIToken>(result);
#endif
            return token;
        }

        public static void VerifyToken(ref MSIToken token)
        {
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
