using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LogicAppAdvancedTool
{
    public class HttpOperations
    {
        public static string ValidatedHttpRequestWithToken(string url, HttpMethod method, string content, string token, string exceptionMessage)
        {
            HttpResponseMessage response = HttpRequestWithToken(url, method, content, token);

            string responseMessage = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                throw new ExpectedException($"{exceptionMessage}, status code {response.StatusCode}\r\nDetail message:{responseMessage}");
            }

            return responseMessage;
        }

        public static HttpResponseMessage HttpRequestWithToken(string url, HttpMethod method, string content, string token)
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

            return response;
        }

        public async static Task<HttpResponseMessage> HttpRequestWithTokenAsync(string url, HttpMethod method, string content, string token)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");


            HttpRequestMessage requestMessage = new HttpRequestMessage(method, url);

            if (content != null)
            {
                requestMessage.Content = new StringContent(content, Encoding.UTF8, "application/json");
            }

            HttpResponseMessage response = await client.SendAsync(requestMessage);

            return response;
        }
    }
}
