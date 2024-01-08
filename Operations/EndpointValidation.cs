using System;
using System.Net;

namespace LogicAppAdvancedTool.Operations
{
    public static class EndpointValidation
    {
        public static void Run(string endpoint)
        {
            string endpointType = "https";
            int port = 443;

            if (endpoint.StartsWith("http://"))
            { 
                endpointType = "http";
                port = 80;

                Console.WriteLine($"The endpoint {endpoint} you provide is Http protocol, SSL certificate validation will be skipped.");
            }

            string fixedUrl = endpoint.Replace("http://", "").Replace("https://", "").Trim('/');  //format url

            ValidationStatus sslValidationStatus = ValidationStatus.Skipped;
            if (endpointType == "https")
            { 
                sslValidationStatus = new SSLValidator($"https://{fixedUrl}").Validate().Result;
            }

            DNSValidator dnsValidator = new DNSValidator(fixedUrl).Validate();
            
            ConsoleTable consoleTable = new ConsoleTable("Name Resolution", "IP", "TCP connection", "SSL connection");

            if (dnsValidator.Result == ValidationStatus.Succeeded)
            {
                foreach (IPAddress ip in dnsValidator.IPs)
                { 
                    SocketValidator socketValidator = new SocketValidator(ip, port).Validate();

                    consoleTable.AddRow(ValidationStatus.Succeeded.ToString(), ip.ToString(), socketValidator.Result.ToString(), sslValidationStatus.ToString());
                }
            }
            else
            {
                consoleTable.AddRow(ValidationStatus.Failed.ToString(), "N/A", ValidationStatus.NotApplicable.ToString(), ValidationStatus.NotApplicable.ToString());
            }

            consoleTable.Print();
        }
    }
}
