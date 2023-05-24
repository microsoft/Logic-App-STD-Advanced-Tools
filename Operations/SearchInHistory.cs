using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        private static void SearchInHistory(string logicAppName, string workflowName, string date, string keyword)
        {
            string prefix = GenerateWorkflowTablePrefix(logicAppName, workflowName);

            string actionTableName = $"flow{prefix}{date}t000000zactions";

            //Double check whether the action table exists
            TableServiceClient serviceClient = new TableServiceClient(AppSettings.ConnectionString);
            Pageable<TableItem> results = serviceClient.Query(filter: $"TableName eq '{actionTableName}'");

            if (results.Count() == 0)
            {
                throw new UserInputException($"action table - {actionTableName} not exist, please check whether Date is correct.");
            }

            Console.WriteLine($"action table - {actionTableName} found, retrieving action logs...");

            TableClient tableClient = new TableClient(AppSettings.ConnectionString, actionTableName);
            List<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: "InputsLinkCompressed ne '' or OutputsLinkCompressed ne ''").ToList();

            List<TableEntity> filteredEntities = new List<TableEntity>();
            foreach (TableEntity tableEntity in tableEntities) 
            {
                string inputContent = DecodeActionPayloadAsString(tableEntity.GetBinary("InputsLinkCompressed"));
                string outputContent = DecodeActionPayloadAsString(tableEntity.GetBinary("OutputsLinkCompressed"));

                if (SearchForKeyword(inputContent, keyword) || SearchForKeyword(outputContent, keyword))
                {
                    filteredEntities.Add(tableEntity);
                }
            }

            if (filteredEntities.Count == 0)
            {
                throw new UserInputException($"No run hisotry input/output found with keyword {keyword}");
            }

            string fileName = $"{logicAppName}_{workflowName}_{date}_SearchResults.json";

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

            string logFolder = $"{Directory.GetCurrentDirectory()}/FilteredLogs";

            if (!Directory.Exists(logFolder))
            {
                Directory.CreateDirectory(logFolder);
            }

            string filePath = $"{logFolder}/{fileName}";

            if (File.Exists(filePath))
            {
                File.Delete(filePath);

                Console.WriteLine($"File already exists, the previous log file has been deleted");
            }

            File.AppendAllText(filePath, JsonConvert.SerializeObject(records, Formatting.Indented));
            Console.WriteLine($"Log generated, please check the file - {filePath}");
        }
    }
}
