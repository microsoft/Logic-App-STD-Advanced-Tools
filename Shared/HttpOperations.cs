using System.IO;
using System.Net;
using System.Text;

namespace LogicAppAdvancedTool
{
    public class HttpOperations
    {
        public static string HttpGetWithToken(string Url, string method, string token, string exceptionMessage)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            request.Method = method;
            request.Headers.Clear();
            request.Headers.Add("Authorization", $"Bearer {token}");

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                { 
                    return sr.ReadToEnd();
                }
            }
            catch (WebException ex)
            {
                HttpWebResponse response = ex.Response as HttpWebResponse;

                string errorResponse;
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    errorResponse = sr.ReadToEnd();
                }

                throw new ExpectedException($"{exceptionMessage}, status code {response.StatusCode}\r\nDetail message:{errorResponse}");
            }
        }

        public static void HttpSendWithToken(string Url, string method, string payload, string token, string exceptionMessage)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            request.Method = method;
            request.Headers.Clear();
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.ContentType = "application/json";

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(payload);

                Stream requestStream = request.GetRequestStream();
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException ex)
            {
                HttpWebResponse response = ex.Response as HttpWebResponse;

                string errorResponse;
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    errorResponse = sr.ReadToEnd();
                }

                throw new ExpectedException($"{exceptionMessage}, status code {response.StatusCode}\r\nDetail message:{errorResponse}");
            }
        }
    }
}
