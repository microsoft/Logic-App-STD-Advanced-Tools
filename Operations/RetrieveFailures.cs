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
        private static void RetrieveFailuresByDate(string logicAppName, string workflowName, string date)
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
            List<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: "Status eq 'Failed'").ToList();

            string fileName = $"{logicAppName}_{workflowName}_{date}_FailureLogs.json";

            SaveFailureLogs(tableEntities, fileName);
        }

        private static void RetrieveFailuresByRun(string logicAppName, string workflowName, string runID)
        {
            string prefix = GenerateWorkflowTablePrefix(logicAppName, workflowName);
            string runTableName = $"flow{prefix}runs";

            //Double check whether the action table exists
            TableServiceClient serviceClient = new TableServiceClient(AppSettings.ConnectionString);
            Pageable<TableItem> results = serviceClient.Query(filter: $"TableName eq '{runTableName}'");

            if (results.Count() == 0)
            {
                throw new UserInputException($"run table - {runTableName} not exist, please check whether Date is correct.");
            }

            Console.WriteLine($"run table - {runTableName} found, retrieving run history logs...");

            TableClient tableClient = new TableClient(AppSettings.ConnectionString, runTableName);
            List<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: $"FlowRunSequenceId eq '{runID}'").ToList();

            if (tableEntities.Count == 0)
            {
                throw new UserInputException($"Cannot find workflow run with run id: {runID} of workflow: {workflowName}, please check your input.");
            }

            Console.WriteLine($"Workflow run id found in run history table. Retrieving failure actions.");

            string runTime = tableEntities.First().GetDateTimeOffset("CreatedTime")?.ToString("yyyyMMdd");
            string actionTableName = $"flow{prefix}{runTime}t000000zactions";

            tableClient = new TableClient(AppSettings.ConnectionString, actionTableName);
            tableEntities = tableClient.Query<TableEntity>(filter: $"Status eq 'Failed' and FlowRunSequenceId eq '{runID}'").ToList();

            string fileName = $"{logicAppName}_{workflowName}_{runID}_FailureLogs.json";

            SaveFailureLogs(tableEntities, fileName);
        }

        private static void SaveFailureLogs(List<TableEntity> tableEntities, string fileName)
        {
            if (tableEntities.Count == 0)
            {
                throw new UserInputException("No failure actions found in action table.");
            }

            Dictionary<string, List<HistoryRecords>> records = new Dictionary<string, List<HistoryRecords>>();

            //Insert all the failure records as per RunID
            foreach (TableEntity entity in tableEntities)
            {
                //Ignore the failed actions which don't have input and output, mostly they are control action like foreach, until
                if (entity.GetBinary("InputsLinkCompressed") == null && entity.GetBinary("OutputsLinkCompressed") == null && entity.GetBinary("Error") == null)
                {
                    continue;
                }

                string runID = entity.GetString("FlowRunSequenceId");

                HistoryRecords failureRecords = new HistoryRecords(entity);

                if (failureRecords.Error != null && failureRecords.Error.Message.Contains("An action failed. No dependent actions succeeded."))
                {
                    continue;       //exclude actions (eg:foreach, until) which failed due to inner actions.
                }

                if (!records.ContainsKey(runID))
                {
                    records.Add(runID, new List<HistoryRecords>());
                }

                records[runID].Add(failureRecords);
            }

            string logFolder = $"{Directory.GetCurrentDirectory()}/FailureLogs";

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
            Console.WriteLine($"Failure log generated, please check the file - {filePath}");
        }
    }
}
