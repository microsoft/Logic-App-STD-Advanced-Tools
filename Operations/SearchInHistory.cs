using Azure.Data.Tables;
using LogicAppAdvancedTool.Shared;
using LogicAppAdvancedTool.Structures;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace LogicAppAdvancedTool.Operations
{
    public static class SearchInHistory
    {
        public static void Run(string workflowName, string date, string keyword, bool includeBlob = false)
        {
            string selectedWorkflowId = WorkflowSelector.SelectFlowIDByName(workflowName);

            List<TableEntity> tableEntities = new List<TableEntity>();

            List<TableEntity> filteredEntities = new List<TableEntity>();
            List<string> runIDs = new List<string>();

            int index = 0;

            string tableName = $"flow{CommonOperations.GenerateWorkflowTablePrefixByFlowID(selectedWorkflowId)}{date}t000000zactions";
            string query = "InputsLinkCompressed ne '' or OutputsLinkCompressed ne ''";

            PageableTableQuery pageableTableQuery = new PageableTableQuery(AppSettings.ConnectionString, tableName, query);
            while (pageableTableQuery.HasNextPage)
            {
                Console.WriteLine($"Processing page {++index}");

                tableEntities = pageableTableQuery.GetNextPage();

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
            }

            if (filteredEntities.Count == 0)
            {
                throw new UserInputException($"No run hisotry input/output found with keyword {keyword}");
            }

            string fileName = $"{AppSettings.LogicAppName}_{workflowName}_{date}_SearchResults.json";

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
