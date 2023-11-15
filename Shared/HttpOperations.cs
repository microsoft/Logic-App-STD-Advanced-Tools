using System;
using System.Net.Http;
using System.Text;

namespace LogicAppAdvancedTool
{
    public class HttpOperations
    {
        public static string HttpRequestWithToken(string url, HttpMethod method, string content, string token, string exceptionMessage)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");


            HttpRequestMessage requestMessage = new HttpRequestMessage(method, url);

            if (content != null)
            {
                requestMessage.Content = new StringContent(content, Encoding.UTF8, "application/json");
            }

            HttpResponseMessage response = client.Send(requestMessage);

            string responseMessage = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                throw new ExpectedException($"{exceptionMessage}, status code {response.StatusCode}\r\nDetail message:{responseMessage}");
            }

            return responseMessage;
        }
    }
}
