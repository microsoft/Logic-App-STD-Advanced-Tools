using System;
using System.Collections.Generic;
using McMaster.Extensions.CommandLineUtils;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Linq;
using LogicAppAdvancedTool.Structures;

namespace LogicAppAdvancedTool.Operations
{
    public static class BatchResubmit
    {
        public static void Run(string workflowName, string startTime, string endTime, bool ignoreProcessed, string status)
        {
            string baseUrl = $"https://management.azure.com/subscriptions/{AppSettings.SubscriptionID}/resourceGroups/{AppSettings.ResourceGroup}/providers/Microsoft.Web/sites/{AppSettings.LogicAppName}/hostruntime/runtime/webhooks/workflow/api/management/workflows/{workflowName}";
            string filter = $"$filter=status eq '{status}' and startTime gt {startTime} and startTime lt {endTime}";

            string listRunUrl = $"{baseUrl}/runs?api-version=2018-11-01&{filter}";
            MSIToken token = MSITokenService.RetrieveToken("https://management.azure.com");
            Console.WriteLine("Managed Identity token retrieved");

            //Create log file for processed run ids based on provided parameters
            //Resubmit execution might be unexpected terminated due to Logic App runtime reboot, so use log file to store all processed runs to avoid resubmit same failed run multiple times
            string logPath = $"BatchResubmit_{workflowName}_{status}_{DateTime.Parse(startTime).ToString("yyyyMMddHHmmss")}_{DateTime.Parse(endTime).ToString("yyyyMMddHHmmss")}.log";

            List<string> processedRuns = new List<string>();

            if (ignoreProcessed)
            {
                Console.WriteLine($"Detected setting to ignore resubmitted {status} runs, loading {logPath} for resubmitted records");

                if (File.Exists(logPath))
                {
                    string processedContent = File.ReadAllText(logPath);

                    processedRuns = processedContent.TrimEnd('\n').Split('\n').ToList();
                    processedRuns.RemoveAll(s => string.IsNullOrEmpty(s));

                    Console.WriteLine($"{processedRuns.Count} records founds, will ignore those runs.");
                }
                else
                {
                    Console.WriteLine("Resubmitted records file not found, will resubmit all failed runs.");
                }

            }

            List<RunInfo> remainRuns = new List<RunInfo>();

            while (!String.IsNullOrEmpty(listRunUrl))
            {
                MSITokenService.VerifyToken(ref token);

                string content = HttpOperations.HttpRequestWithToken(listRunUrl, "GET", null, token.access_token, "Failed to retrieve failed runs");
                JObject rawResponse = JObject.Parse(content);
                List<JToken> runs = rawResponse["value"].ToObject<List<JToken>>();

                foreach (JToken run in runs)
                {
                    if (ignoreProcessed)
                    {
                        if (processedRuns.Contains(run["name"].ToString()))
                        {
                            continue;
                        }
                    }

                    remainRuns.Add(new RunInfo(run["name"].ToString(), run["properties"]?["trigger"]?["name"].ToString()));
                }

                listRunUrl = rawResponse["nextLink"]?.ToString();
            }

            if (remainRuns.Count == 0)
            {
                throw new ExpectedException("No failed run detected.");
            }

            Console.WriteLine($"Detected {remainRuns.Count} {status} runs.");

            string confirmationMessage = "WARNING!!!\r\nAre you sure to resubmit all detected failed runs?\r\nPlease input for confirmation:";
            if (!Prompt.GetYesNo(confirmationMessage, false, ConsoleColor.Red))
            {
                throw new UserCanceledException("Operation Cancelled");
            }

            while (remainRuns.Count != 0)
            {
                Console.WriteLine($"Start to resubmit {status} runs, remain {remainRuns.Count} runs.");

                for (int i = remainRuns.Count - 1; i >= 0; i--)
                {
                    RunInfo info = remainRuns[i];

                    string resubmitUrl = $"{baseUrl}/triggers/{info.Trigger}/histories/{info.RunID}/resubmit?api-version=2018-11-01";

                    try
                    {
                        MSITokenService.VerifyToken(ref token);

                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(resubmitUrl);
                        request.Method = "POST";
                        request.Headers.Clear();
                        request.Headers.Add("Authorization", $"Bearer {token.access_token}");

                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                        File.AppendAllText(logPath, $"{info.RunID}\n");
                        remainRuns.RemoveAt(i);
                    }
                    catch (WebException ex)
                    {
                        if (ex.Message.Contains("Too Many Requests"))
                        {
                            int delayInterval = 120;

                            Console.WriteLine($"Hit throttling limitation of Azure management API, pause for {delayInterval} seconds and then continue. Still have {remainRuns.Count} runs need to be resubmitted");

                            Thread.Sleep(delayInterval * 1000);

                            break;
                        }
                        else
                        {
                            throw ex;
                        }
                    }
                }
            }

            Console.WriteLine($"All {status} run resubmitted successfully");
        }

        private class RunInfo
        {
            public string RunID { get; private set; }
            public string Trigger { get; private set; }

            public RunInfo(string RunID, string Trigger)
            {
                this.RunID = RunID;
                this.Trigger = Trigger;
            }
        }
    }
}
