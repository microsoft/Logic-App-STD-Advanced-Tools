using LogicAppAdvancedTool.Structures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

namespace LogicAppAdvancedTool.Operations
{
    public static class ValidateWorkflows
    {
        public static void Run()
        {
            DirectoryInfo rootDirectory = new DirectoryInfo(AppSettings.RootFolder);
            List<DirectoryInfo> workflowsDirectories = rootDirectory.GetDirectories().Where(s => File.Exists($"{s.FullName}\\workflow.json")).ToList();

            if (workflowsDirectories.Count == 0)
            {
                throw new ExpectedException("No workflows found in Logic App.");
            }

            Console.WriteLine($"Found {workflowsDirectories.Count} workflow(s), start to validate...");

            MSIToken token = MSITokenService.RetrieveToken("https://management.azure.com");

            StringBuilder result = new StringBuilder();

            foreach (DirectoryInfo di in workflowsDirectories)
            { 
                string definition = "{\"properties\":" + File.ReadAllText($"{di.FullName}\\workflow.json") + "}";

                string validationUrl = $"{AppSettings.ManagementBaseUrl}/hostruntime/runtime/webhooks/workflow/api/management/workflows/{di.Name}/validate?api-version=2018-11-01";

                HttpResponseMessage response = HttpOperations.HttpRequestWithToken(validationUrl, HttpMethod.Post, definition, token.access_token);
                if (!response.IsSuccessStatusCode)
                {
                    string responseMessage = response.Content.ReadAsStringAsync().Result;

                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        result.AppendLine($"{di.Name}: Validation failed - Exception message: {responseMessage}");
                    }
                    else
                    {
                        throw new ExpectedException($"Failed to validate , status code {response.StatusCode}\r\nDetail message:{responseMessage}");
                    }
                }
                else
                {
                    result.AppendLine($"{di.Name}: Vaildation passed.");
                }
            }

            Console.Write(result.ToString());
        }
    }
}
