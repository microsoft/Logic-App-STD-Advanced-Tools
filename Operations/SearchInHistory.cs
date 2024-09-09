using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Globalization;
using LogicAppAdvancedTool.Structures;
using System.Linq;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;

namespace LogicAppAdvancedTool.Operations
{
    public static class SearchInHistory
    {
        public static void Run(string workflowName, string date, string keyword, bool includeBlob = false, bool onlyFailures = false)
        {
            Console.WriteLine($"Retrieving all workflows named {workflowName} based on workflow ID.");

            List<TableEntity> entitiesOfWorkflow = TableOperations.QueryMainTable($"FlowName eq '{workflowName}'", select: new string[] { "RowKey", "FlowUpdatedTime", "FlowId", "Kind" })
                                        .GroupBy(t => t.GetString("FlowId"))
                                        .Select(g => g.OrderByAscending(
                                            x => x.GetDateTimeOffset("FlowUpdatedTime"))
                                            .FirstOrDefault())
                                        .ToList();

            if (entitiesOfWorkflow.Count == 0)
            {
                throw new UserInputException($"{workflowName} cannot be found in storage table, please check whether workflow is correct.");
            }

            string selectedWorkflowId = string.Empty;
            if (entitiesOfWorkflow.Count == 1)
            {
                selectedWorkflowId = entitiesOfWorkflow[0].GetString("FlowId");
                Console.WriteLine($"Only one workflow named {workflowName} found, auto select id {selectedWorkflowId}.");
            }
            else
            {
                TableEntity currentWorkflow = TableOperations.QueryMainTable($"RowKey eq 'MYEDGEENVIRONMENT_FLOWLOOKUP-MYEDGERESOURCEGROUP-{workflowName.ToUpper()}'", select: new string[] { "FlowId" }).FirstOrDefault();

                string currentFlowID = string.Empty;
                if (currentWorkflow != null)
                {
                    currentFlowID = currentWorkflow.GetString("FlowId");
                }

                ConsoleTable workflowTable = new ConsoleTable(new List<string>() { "Flow ID", "Created Time", "Kind", "Status" }, true);

                foreach (TableEntity entity in entitiesOfWorkflow)
                {
                    workflowTable.AddRow(new List<string>() { entity.GetString("FlowId"), entity.GetDateTimeOffset("FlowUpdatedTime")?.ToString("yyyy-MM-ddTHH:mm:ssZ"), entity.GetString("Kind"), currentFlowID == entity.GetString("FlowId") ? "In Use" : "Deleted" });
                }

                workflowTable.Print();

                Console.WriteLine("Please select the workflow you want to restore by entering the index.");

                int selectedIndex = int.Parse(Console.ReadLine());
                selectedWorkflowId = entitiesOfWorkflow[selectedIndex - 1].GetString("FlowId");
            }

            List<TableEntity> tableEntities = new List<TableEntity>();

            if (onlyFailures)
            {
                DateTime minTimeStamp = DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture);
                DateTime maxTimeStamp = minTimeStamp.AddDays(1);

                string query = $"Status eq 'Failed' and CreatedTime ge datetime'{minTimeStamp.ToString("yyyy-MM-ddTHH:mm:ssZ")}' and EndTime le datetime'{maxTimeStamp.ToString("yyyy-MM-ddTHH:mm:ssZ")}'";
                List<TableEntity> failedRuns = TableOperations.QueryRunTableByFlowID(selectedWorkflowId, query, new string[] { "FlowRunSequenceId" });

                if (failedRuns.Count == 0)
                {
                    throw new UserInputException($"There's no failed run found of {workflowName} with flow id {selectedWorkflowId} on {date}");
                }

                Console.WriteLine($"Found {failedRuns.Count} failed run(s) in run table.");

                foreach (TableEntity te in failedRuns)
                {
                    string runID = te.GetString("FlowRunSequenceId");

                    tableEntities.AddRange(TableOperations.QueryActionTableByFlowID(selectedWorkflowId, date, $"(InputsLinkCompressed ne '' or OutputsLinkCompressed ne '') and FlowRunSequenceId eq '{runID}'"));
                }
            }
            else
            {
                tableEntities = TableOperations.QueryActionTableByFlowID(selectedWorkflowId, date, "InputsLinkCompressed ne '' or OutputsLinkCompressed ne ''");
            }

            List<TableEntity> filteredEntities = new List<TableEntity>();
            List<string> runIDs = new List<string>();

            foreach (TableEntity tableEntity in tableEntities)
            {
                ContentDecoder inputDecoder = new ContentDecoder(tableEntity.GetBinary("InputsLinkCompressed"));
                ContentDecoder outputDecoder = new ContentDecoder(tableEntity.GetBinary("OutputsLinkCompressed"));

                if (inputDecoder.SearchKeyword(keyword, includeBlob) || outputDecoder.SearchKeyword(keyword, includeBlob))
                {
                    filteredEntities.Add(tableEntity);

                    string runID = tableEntity.GetString("FlowRunSequenceId");
                    if (!runIDs.Contains(runID))
                    {
                        runIDs.Add(runID);
                    }
                }
            }

            if (filteredEntities.Count == 0)
            {
                throw new UserInputException($"No run hisotry input/output found with keyword {keyword}");
            }

            string fileName = $"{AppSettings.LogicAppName}_{workflowName}_{date}_SearchResults_{keyword}.json";

            ConsoleTable runIdTable = new ConsoleTable(new List<string>() { "Run ID" });
            foreach (string id in runIDs)
            {
                runIdTable.AddRow(new List<string>() { id });
            }

            runIdTable.Print();

            SaveLogs(filteredEntities, fileName);
        }

        private static bool SearchForKeyword(string content, string keyword)
        {
            if (!String.IsNullOrEmpty(content))
            {
                return content.Contains(keyword);
            }

            return false;
        }
        private static void SaveLogs(List<TableEntity> tableEntities, string fileName)
        {
            Dictionary<string, List<HistoryRecords>> records = new Dictionary<string, List<HistoryRecords>>();

            foreach (TableEntity entity in tableEntities)
            {
                string runID = entity.GetString("FlowRunSequenceId");

                HistoryRecords filteredRecords = new HistoryRecords(entity);

                if (!records.ContainsKey(runID))
                {
                    records.Add(runID, new List<HistoryRecords>());
                }

                records[runID].Add(filteredRecords);
            }

            if (File.Exists(fileName))
            {
                File.Delete(fileName);

                Console.WriteLine($"File already exists, the previous log file has been deleted");
            }

            File.AppendAllText(fileName, JsonConvert.SerializeObject(records, Formatting.Indented));
            Console.WriteLine($"Log generated, please check the file - {fileName}");
        }
    }
}
