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
            string result = "{\"access_token\":\"eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6InEtMjNmYWxldlpoaEQzaG05Q1Fia1A1TVF5VSIsImtpZCI6InEtMjNmYWxldlpoaEQzaG05Q1Fia1A1TVF5VSJ9.eyJhdWQiOiJodHRwczovL21hbmFnZW1lbnQuYXp1cmUuY29tIiwiaXNzIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvNzJmOTg4YmYtODZmMS00MWFmLTkxYWItMmQ3Y2QwMTFkYjQ3LyIsImlhdCI6MTcxMzc1NzM5NCwibmJmIjoxNzEzNzU3Mzk0LCJleHAiOjE3MTM4NDQwOTQsImFpbyI6IkUyTmdZUGphMEgybDEzbjloL1RMSzE1ek9JZWZBZ0E9IiwiYXBwaWQiOiIzODVkN2NmYi0zMWVjLTQyMDYtODk1NC04Y2NmZWIzYzE2ZmUiLCJhcHBpZGFjciI6IjIiLCJpZHAiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC83MmY5ODhiZi04NmYxLTQxYWYtOTFhYi0yZDdjZDAxMWRiNDcvIiwiaWR0eXAiOiJhcHAiLCJvaWQiOiIzYzI0OTU5ZS05ZTFmLTQ4YzItYjQ2OC00ZmM5ZmIzMDZjNTkiLCJyaCI6IjAuQVJvQXY0ajVjdkdHcjBHUnF5MTgwQkhiUjBaSWYza0F1dGRQdWtQYXdmajJNQk1hQUFBLiIsInN1YiI6IjNjMjQ5NTllLTllMWYtNDhjMi1iNDY4LTRmYzlmYjMwNmM1OSIsInRpZCI6IjcyZjk4OGJmLTg2ZjEtNDFhZi05MWFiLTJkN2NkMDExZGI0NyIsInV0aSI6IlFnWnotTnVIVWsyY19PVkgxTjF3QUEiLCJ2ZXIiOiIxLjAiLCJ4bXNfbWlyaWQiOiIvc3Vic2NyaXB0aW9ucy9hYWZiMjdkZS1lNzI4LTRjYjgtOWU2OC05YWZkZTFkMWE4NDkvcmVzb3VyY2Vncm91cHMvTG9naWNBcHBTdGFuZGFyZC9wcm92aWRlcnMvTWljcm9zb2Z0LldlYi9zaXRlcy9EcmFjTG9naWNBcHAiLCJ4bXNfdGNkdCI6IjEyODkyNDE1NDcifQ.WwVoxLwy3iU0CA2sYEl_DSM3OlrQwsyQUj1MKq4dzjSh6fg30lEBE-jGg_zPpLejNcJGNc351lt1RllmGvbiHUc4HzJbHAYImk8vrNrn5Dt-VEnTHPSf2oVUw7ZtIhDhARNWp-5bVAtBF3R0YNAWcBM1GFvMSktcqevx65T-YDA99NrUs-Fn0MV2MBeVXjeJX2MNxaT4XgGW1LDMWsXwvqPBqRixjfqeEaT3kf7T1B5MGCavaoxXcruR0L19IO5ybeVNw-t72_Z0kAAp1m5fMc_MYsXtAs4xuG_Z9c48iIsnz38i0tKxZGjLnxEhTro_d79pIcpuS8nJFLcj0nOcIQ\",\"expires_on\":\"1694141276\",\"resource\":\"https://management.azure.com\",\"token_type\":\"Bearer\",\"client_id\":\"5D1FE1E8-B908-419B-9BAA-6D9BAEA11D75\"}";
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
