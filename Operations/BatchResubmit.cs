﻿using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using McMaster.Extensions.CommandLineUtils;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using System.Threading;
using System.Linq;
using static LogicAppAdvancedTool.MSITokenService;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        private static void BatchResubmit(string workflowName, string startTime, string endTime, bool ignoreProcessed)
        {
            string baseUrl = $"https://management.azure.com/subscriptions/{AppSettings.SubscriptionID}/resourceGroups/{AppSettings.ResourceGroup}/providers/Microsoft.Web/sites/{AppSettings.LogicAppName}/hostruntime/runtime/webhooks/workflow/api/management/workflows/{workflowName}";
            string filter = $"$filter=status eq 'Failed' and startTime gt {startTime} and startTime lt {endTime}";

            string listFailedRunUrl = $"{baseUrl}/runs?api-version=2018-11-01&{filter}";
            MSIToken token = MSITokenService.RetrieveToken("https://management.azure.com");
            Console.WriteLine("Managed Identity token retrieved");

            List<RunInfo> failedRuns = new List<RunInfo>();

            string logPath = $"BatchResubmit_{workflowName}_{DateTime.Parse(startTime).ToString("yyyyMMddHHmmss")}_{DateTime.Parse(endTime).ToString("yyyyMMddHHmmss")}";

            List<string> processedRuns = new List<string>();

            if (ignoreProcessed)
            {
                Console.WriteLine($"Detected setting to ignore resubmitted failed runs, loading {logPath} for resubmitted records");

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

            while (!String.IsNullOrEmpty(listFailedRunUrl))
            {
                VerifyToken(ref token);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(listFailedRunUrl);
                request.Method = "GET";
                request.Headers.Clear();
                request.Headers.Add("Authorization", $"Bearer {token.access_token}");

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                    {
                        string content = sr.ReadToEnd();

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

                            failedRuns.Add(new RunInfo(run["name"].ToString(), run["properties"]?["trigger"]?["name"].ToString()));
                        }

                        listFailedRunUrl = rawResponse["nextLink"]?.ToString();
                    }
                }
            }

            if (failedRuns.Count == 0)
            {
                throw new ExpectedException("No failed run detected.");
            }

            Console.WriteLine($"Detected {failedRuns.Count} failed runs.");

            string confirmationMessage = "WARNING!!!\r\nAre you sure to resubmit all detected failed runs?\r\nPlease input for confirmation:";
            if (!Prompt.GetYesNo(confirmationMessage, false, ConsoleColor.Red))
            {
                throw new UserCanceledException("Operation Cancelled");
            }

            while (failedRuns.Count != 0)
            {
                Console.WriteLine($"Start to resubmit failed runs, remain {failedRuns.Count} runs.");

                for (int i = failedRuns.Count - 1; i>=0; i--)
                {
                    RunInfo info = failedRuns[i];

                    string resubmitUrl = $"{baseUrl}/triggers/{info.Trigger}/histories/{info.RunID}/resubmit?api-version=2018-11-01";

                    try
                    {
                        VerifyToken(ref token);

                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(resubmitUrl);
                        request.Method = "POST";
                        request.Headers.Clear();
                        request.Headers.Add("Authorization", $"Bearer {token.access_token}");

                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                        File.AppendAllText(logPath, $"{info.RunID}\n");
                        failedRuns.RemoveAt(i);
                    }
                    catch (WebException ex)
                    {
                        if (ex.Message.Contains("Too Many Requests"))
                        {
                            int delayInterval = 120;

                            Console.WriteLine($"Hit throttling limitation of Azure management API, pause for {delayInterval} seconds and then continue. Still have {failedRuns.Count} runs need to be resubmitted");

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

            Console.WriteLine("All failed run resubmitted successfully");
        }

        private static void VerifyToken(ref MSIToken token)
        {
            long epochNow = DateTime.UtcNow.ToEpoch();
            long diff = long.Parse(token.expires_on) - epochNow;

            if (diff < 300) 
            {
                Console.WriteLine($"MSI token will be expired in {diff} seconds, refresh token.");

                token = RetrieveToken("https://management.azure.com");
            }
        }

        private class RunInfo
        { 
            public string RunID { get; private set; }
            public string Trigger { get; private set;}

            public RunInfo(string RunID, string Trigger) 
            { 
                this.RunID = RunID;
                this.Trigger = Trigger;
            }
        }
    }
}
