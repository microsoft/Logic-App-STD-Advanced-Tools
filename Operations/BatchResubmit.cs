using Azure.Data.Tables;
using LogicAppAdvancedTool.Structures;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;

namespace LogicAppAdvancedTool.Operations
{
    public static class BatchResubmit
    {
        public static void Run(string workflowName, string startTime, string endTime, bool ignoreProcessed, string status, string actionName, string keyword)
        {
            CommonOperations.PromptConfirmation("1. Status will be ignored if actionName and keyword provided\r\n" +
                                                "2. If keyword contains space, add double quotes before and after\r\n" +
                                                "3. Action name is case sensitive\r\n" +
                                                "4. When using json content as keyword, remove the sapce after ':', eg: \"id\": 1 -> \"id\":1\r\n" +
                                                "5. Before execute the command, please make sure that the Logic App managed identity has following permission on resource group level:\r\n\tReader\r\n\tLogic App Standard Contributor");

            status = char.ToUpper(status[0]) + status.Substring(1);     //Convert first character to uppercase for table query
            Dictionary<string, List<string>> runRefs = RetrieveRunIDs(workflowName, startTime, endTime, status, actionName, keyword);

            if (runRefs.Count == 0)
            {
                throw new ExpectedException($"No runs found based on parameters, operation cancelled");
            }

            List<string> workflowVersions = runRefs.Keys.ToList();
            Dictionary<string, string> triggerRef = GetTriggersByVersion(workflowName, workflowVersions);

            string baseUrl = $"{AppSettings.ManagementBaseUrl}/hostruntime/runtime/webhooks/workflow/api/management/workflows/{workflowName}";
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

            foreach (string version in runRefs.Keys)
            {
                foreach (string runID in runRefs[version])
                {
                    if (ignoreProcessed && processedRuns.Contains(runID))
                    {
                        continue;
                    }

                    string triggerName = triggerRef[version];
                    remainRuns.Add(new RunInfo(runID, triggerName));
                }
            }

            if (remainRuns.Count == 0)
            {
                throw new ExpectedException("No runs need to be resubmitted.");
            }

            Console.WriteLine($"Detected {remainRuns.Count} {status} runs.");

            CommonOperations.PromptConfirmation("Are you sure to resubmit all detected failed runs?");

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

                        HttpResponseMessage response = HttpOperations.HttpRequestWithToken(resubmitUrl, HttpMethod.Post, null, token.access_token);

                        if (!response.IsSuccessStatusCode)
                        {
                            throw new Exception(response.ReasonPhrase);
                        }

                        File.AppendAllText(logPath, $"{info.RunID}\n");
                        remainRuns.RemoveAt(i);
                    }
                    catch (Exception ex)
                    {
                        //Handle throttling limitation internally since it is expected when we need to resubmit large amount of runs
                        if (ex.Message.Contains("Too Many Requests"))
                        {
                            int delayInterval = 60;

                            Console.WriteLine($"Hit throttling limitation of Azure management API, pause for {delayInterval} seconds and then continue. Still have {remainRuns.Count} runs need to be resubmitted");

                            Thread.Sleep(delayInterval * 1000);

                            break;
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }

            Console.WriteLine($"All {status} run resubmitted successfully");
        }

        private static Dictionary<string, List<string>> RetrieveRunIDs(string workflowName, string startTime, string endTime, string status, string actionName, string keyword)
        { 
            Dictionary<string, List<string>> workflowRuns = new Dictionary<string, List<string>>();

            if (!String.IsNullOrEmpty(actionName) && !String.IsNullOrEmpty(keyword))
            {
                workflowRuns = FilterRunIDsViaKeyWords(workflowName, startTime, endTime, status, actionName.Replace(" ", "_"), keyword);
            }
            else
            { 
                workflowRuns = RetrieveRunIDsWithoutFilter(workflowName, startTime, endTime, status);
            }

            return workflowRuns;
        }

        private static Dictionary<string, List<string>> RetrieveRunIDsWithoutFilter(string workflowName, string startTime, string endTime, string status)
        {
            Dictionary<string, List<string>> workflowRuns = new Dictionary<string, List<string>>();

            string filter = $"Status eq '{status}' and (CreatedTime ge datetime'{startTime}' and CreatedTime le datetime'{endTime}')";
            string runTableName = $"flow{StoragePrefixGenerator.GenerateWorkflowTablePrefixByName(workflowName)}runs";

            PageableTableQuery pageableTableQuery = new PageableTableQuery(runTableName, filter, new string[] { "FlowRunSequenceId", "FlowSequenceId" });

            while (pageableTableQuery.HasNextPage)
            {
                Dictionary<string, List<string>> runInfos = pageableTableQuery.GetNextPage()
                                                .Select(x => new { version = x.GetString("FlowSequenceId"), runID = x.GetString("FlowRunSequenceId") })
                                                .GroupBy(y => y.version, y => y.runID)
                                                .ToDictionary(z =>  z.Key,  z => z.ToList() );

                foreach (string key in runInfos.Keys)
                {
                    if (workflowRuns.ContainsKey(key))
                    {
                        workflowRuns[key].AddRange(runInfos[key]);
                    }
                    else
                    { 
                        workflowRuns.Add(key, runInfos[key]);
                    }

                    //quick and dirty implementation, need to verify later based on RowKey for different run status
                    workflowRuns[key] = workflowRuns[key].Distinct().ToList();
                }
            }

            return workflowRuns;
        }

        private static Dictionary<string, List<string>> FilterRunIDsViaKeyWords(string workflowName, string startTime, string endTime, string status, string actionName, string keyWords)
        {
            Dictionary<string, List<string>> workflowRuns = new Dictionary<string, List<string>>();
            List<string> tempRunIDs = new List<string>();

            string stDate = DateTime.Parse(startTime).ToString("yyyyMMdd");
            string etDate = DateTime.Parse(endTime).AddDays(1).ToString("yyyyMMdd");    //add 1 day since TableServiceClient doesn't support query by using startswith/endswith
            string actionTableSTPrefix = $"flow{StoragePrefixGenerator.GenerateWorkflowTablePrefixByName(workflowName)}{stDate}";
            string actionTableETPrefix = $"flow{StoragePrefixGenerator.GenerateWorkflowTablePrefixByName(workflowName)}{etDate}";

            TableServiceClient client = StorageClientCreator.GenerateTableServiceClient();
            List<string> tables = client.Query(filter: $"TableName ge '{actionTableSTPrefix}' and TableName le '{actionTableETPrefix}'")
                                            .Where( x=> x.Name.EndsWith("actions"))
                                            .Select( x=> x.Name)
                                            .ToList();

            Console.WriteLine($"Found {tables.Count} table(s) based on workflow named \"{workflowName}\"");

            foreach (string actionTableName in tables)
            {
                string actionQuery = $"ActionName eq '{actionName}' and (CreatedTime ge datetime'{startTime}' and CreatedTime le datetime'{endTime}')";
                PageableTableQuery pageableTableQuery = new PageableTableQuery(actionTableName, actionQuery, new string[] { "OutputsLinkCompressed", "FlowRunSequenceId", "FlowSequenceId" });

                while (pageableTableQuery.HasNextPage)
                {
                    Console.WriteLine($"Filtering page {pageableTableQuery.PageCount} in table {actionTableName}");

                    List<TableEntity> entities = pageableTableQuery.GetNextPage();

                    foreach (TableEntity entity in entities)
                    {
                        string runID = entity.GetString("FlowRunSequenceId");
                        string flowSequenceId = entity.GetString("FlowSequenceId");

                        if (tempRunIDs.Contains(runID))
                        {
                            continue;
                        }

                        //maybe need to check error message as well, but leave for future if have such request
                        ContentDecoder decoder = new ContentDecoder(entity.GetBinary("OutputsLinkCompressed"));

                        if (decoder.SearchKeyword(keyWords))
                        {
                            tempRunIDs.Add(runID);

                            if (workflowRuns.ContainsKey(flowSequenceId))
                            {
                                workflowRuns[flowSequenceId].Add(runID);
                            }
                            else
                            {
                                workflowRuns.Add(flowSequenceId, new List<string>() { runID });
                            }
                        }
                    }
                }
            }

            return workflowRuns;
        }

        private static Dictionary<string, string> GetTriggersByVersion(string workflowName, List<string> versions)
        {
            string historyTableName = $"flow{StoragePrefixGenerator.GenerateWorkflowTablePrefixByName(workflowName)}histories";
            Dictionary<string, string> triggerRefer = new Dictionary<string, string>();

            foreach (string version in versions)
            {
                string filter = $"FlowSequenceId eq '{version}'";

                PageableTableQuery query = new PageableTableQuery(historyTableName, filter, new string[] { "TriggerName"}, 1);   //take 1 for get trigger name

                string triggerName = query.GetNextPage().First().GetString("TriggerName");

                triggerRefer.Add(version, triggerName);
            }


            return triggerRefer;
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
