using Azure.Core;
using Microsoft.WindowsAzure.ResourceStack.Common.Services.ADAuthentication;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace LogicAppAdvancedTool
{
    public class HttpOperations
    {
        public static string HttpRequestWithToken(string url, string method, string content, string token, string exceptionMessage)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            HttpMethod httpMethod;

            switch (method)
            { 
                case "GET":
                    httpMethod = HttpMethod.Get; 
                    break;
                case "POST":
                    httpMethod = HttpMethod.Post;
                    break;
                case "PUT":
                    httpMethod = HttpMethod.Put;
                    break;
                case "DELETE":
                    httpMethod = HttpMethod.Delete;
                    break;
                case "PATCH":
                    httpMethod = HttpMethod.Patch;
                    break;
                default:
                    throw new ArgumentException("Invalid Http Method");
            }

            HttpRequestMessage requestMessage = new HttpRequestMessage(httpMethod, url);

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
