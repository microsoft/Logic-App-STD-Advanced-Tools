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
            TableServiceClient serviceClient = new TableServiceClient(ConnectionString);
            Pageable<TableItem> results = serviceClient.Query(filter: $"TableName eq '{actionTableName}'");

            if (results.Count() == 0)
            {
                throw new UserInputException($"action table - {actionTableName} not exist, please check whether Date is correct.");
            }

            Console.WriteLine($"action table - {actionTableName} found, retrieving action logs...");

            TableClient tableClient = new TableClient(ConnectionString, actionTableName);
            List<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: "Status eq 'Failed'").ToList();

            string fileName = $"{logicAppName}_{workflowName}_{date}_FailureLogs.json";

            SaveFailureLogs(tableEntities, fileName);
        }

        private static void RetrieveFailuresByRun(string logicAppName, string workflowName, string runID)
        {
            string prefix = GenerateWorkflowTablePrefix(logicAppName, workflowName);
            string runTableName = $"flow{prefix}runs";

            //Double check whether the action table exists
            TableServiceClient serviceClient = new TableServiceClient(ConnectionString);
            Pageable<TableItem> results = serviceClient.Query(filter: $"TableName eq '{runTableName}'");

            if (results.Count() == 0)
            {
                throw new UserInputException($"run table - {runTableName} not exist, please check whether Date is correct.");
            }

            Console.WriteLine($"run table - {runTableName} found, retrieving run history logs...");

            TableClient tableClient = new TableClient(ConnectionString, runTableName);
            List<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: $"FlowRunSequenceId eq '{runID}'").ToList();

            if (tableEntities.Count == 0)
            {
                throw new UserInputException($"Cannot find workflow run with run id: {runID} of workflow: {workflowName}, please check your input.");
            }

            Console.WriteLine($"Workflow run id found in run history table. Retrieving failure actions.");

            string runTime = tableEntities.First().GetDateTimeOffset("CreatedTime")?.ToString("yyyyMMdd");
            string actionTableName = $"flow{prefix}{runTime}t000000zactions";

            tableClient = new TableClient(ConnectionString, actionTableName);
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

            Dictionary<string, List<FailureRecords>> records = new Dictionary<string, List<FailureRecords>>();

            //Insert all the failure records as per RunID
            foreach (TableEntity entity in tableEntities)
            {
                //Ignore the failed actions which don't have input and output, mostly they are control action like foreach, until
                if (entity.GetBinary("InputsLinkCompressed") == null && entity.GetBinary("OutputsLinkCompressed") == null && entity.GetBinary("Error") == null)
                {
                    continue;
                }

                string runID = entity.GetString("FlowRunSequenceId");

                FailureRecords failureRecords = new FailureRecords(entity);

                if (failureRecords.Error != null && failureRecords.Error.message.Contains("An action failed. No dependent actions succeeded."))
                {
                    continue;       //exclude actions (eg:foreach, until) which failed due to inner actions.
                }

                if (!records.ContainsKey(runID))
                {
                    records.Add(runID, new List<FailureRecords>());
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

        public class FailureRecords
        {
            public DateTimeOffset Timestamp { get; private set; }
            public string ActionName { get; private set; }
            public string Code { get; private set; }
            public dynamic InputContent { get; private set; }
            public dynamic OutputContent { get; private set; }
            public ActionError Error { get; private set; }
            public string RepeatItemName { get; private set; }
            public int? RepeatItemIdenx { get; private set; }
            public string ActionRepetitionName { get; private set; }

            [JsonIgnore]
            public CommonPayloadStructure InputsLink { get; private set; }
            [JsonIgnore]
            public CommonPayloadStructure OutputsLink { get; private set; }

            public FailureRecords(TableEntity tableEntity)
            {
                this.Timestamp = tableEntity.GetDateTimeOffset("Timestamp") ?? DateTimeOffset.MinValue;
                this.ActionName = tableEntity.GetString("ActionName");
                this.Code = tableEntity.GetString("Code");
                this.RepeatItemName = tableEntity.GetString("RepeatItemScopeName");
                this.RepeatItemIdenx = tableEntity.GetInt32("RepeatItemIndex");
                this.ActionRepetitionName = tableEntity.GetString("ActionRepetitionName");

                this.InputContent = DecodeActionPayload(tableEntity.GetBinary("InputsLinkCompressed"));
                this.OutputContent = DecodeActionPayload(tableEntity.GetBinary("OutputsLinkCompressed"));

                string rawError = DecompressContent(tableEntity.GetBinary("Error"));
                this.Error = String.IsNullOrEmpty(rawError) ? null : JsonConvert.DeserializeObject<ActionError>(rawError);
            }
        }
    }
}
