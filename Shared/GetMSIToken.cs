using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace LogicAppAdvancedTool
{
    class MSITokenService
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
#if !DEBUG
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
#else
            //for local debugging, during development, we cannot get MI of LA, so we have to generate MI token on Kudu and provide here as hardcode (via using Tools GetMIToken command).
            Console.WriteLine("DEBUG mode, use pre-generated token.");
            result = "{\"access_token\":\"eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Ii1LSTNROW5OUjdiUm9meG1lWm9YcWJIWkdldyIsImtpZCI6Ii1LSTNROW5OUjdiUm9meG1lWm9YcWJIWkdldyJ9.eyJhdWQiOiJodHRwczovL21hbmFnZW1lbnQuYXp1cmUuY29tIiwiaXNzIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvNzJmOTg4YmYtODZmMS00MWFmLTkxYWItMmQ3Y2QwMTFkYjQ3LyIsImlhdCI6MTY5NTYxODI5NiwibmJmIjoxNjk1NjE4Mjk2LCJleHAiOjE2OTU3MDQ5OTYsImFpbyI6IkUyRmdZSGozdWlZdHdmQ29DWHU5VDQzNVRWMUdBQT09IiwiYXBwaWQiOiI1ZDFmZTFlOC1iOTA4LTQxOWItOWJhYS02ZDliYWVhMTFkNzUiLCJhcHBpZGFjciI6IjIiLCJpZHAiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC83MmY5ODhiZi04NmYxLTQxYWYtOTFhYi0yZDdjZDAxMWRiNDcvIiwiaWR0eXAiOiJhcHAiLCJvaWQiOiJmNjBlZGI1OC0yOTk3LTRjZTctOTE2NC1kM2MwNWQxZGY0ZWYiLCJyaCI6IjAuQVJvQXY0ajVjdkdHcjBHUnF5MTgwQkhiUjBaSWYza0F1dGRQdWtQYXdmajJNQk1hQUFBLiIsInN1YiI6ImY2MGVkYjU4LTI5OTctNGNlNy05MTY0LWQzYzA1ZDFkZjRlZiIsInRpZCI6IjcyZjk4OGJmLTg2ZjEtNDFhZi05MWFiLTJkN2NkMDExZGI0NyIsInV0aSI6IlQ0Y0J3dlJHdVVlcmlpQzQwOElsQUEiLCJ2ZXIiOiIxLjAiLCJ4bXNfbWlyaWQiOiIvc3Vic2NyaXB0aW9ucy9hYWZiMjdkZS1lNzI4LTRjYjgtOWU2OC05YWZkZTFkMWE4NDkvcmVzb3VyY2Vncm91cHMvTG9naWNBcHBTdGFuZGFyZC9wcm92aWRlcnMvTWljcm9zb2Z0LldlYi9zaXRlcy9EcmFjTG9naWNBcHAiLCJ4bXNfdGNkdCI6IjEyODkyNDE1NDcifQ.ZWQHvlcxbQvOU43P5cSLB8LtHSZ7gZUDzrOqPNnWBYVhGtuQwuaXyBwJEaCVPXtHYAZ3wubmyHxHBHXz8PockUVSw9P8CFurzTZMRiP0ZjJBfWL8rNHHzvbwrH_Z9YnW8wpsS3rnfngiclyc1f55gQjQ67tQEmlZqsz1HZZHLfANhmOi_iISV_zmvbsG_f7j_B4WXQhFyfitaJ7-hjy2bDC65eic66r6VbU8aoTVg8zswfBuOdnZy2Z7GnwQUiK7mI2YOMcGBWjXXb-CqXZzLjD1voSBicnMT7UdgeYlp3rbJ8e9PZqqsS6esHzK2p-k8EgK-TrJ4dXcjc8qu5f07g\",\"expires_on\":\"1694141276\",\"resource\":\"https://management.azure.com\",\"token_type\":\"Bearer\",\"client_id\":\"5D1FE1E8-B908-419B-9BAA-6D9BAEA11D75\"}";      
#endif

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
