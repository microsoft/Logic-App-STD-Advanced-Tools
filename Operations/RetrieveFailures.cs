using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Text;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        private static void RetrieveFailures(string LogicAppName, string WorkflowName, string Date)
        {
            string Prefix = GenerateWorkflowTablePrefix(LogicAppName, WorkflowName);

            string actionTableName = $"flow{Prefix}{Date}t000000zactions";

            //Double check whether the action table exists
            TableServiceClient serviceClient = new TableServiceClient(ConnectionString);
            Pageable<TableItem> results = serviceClient.Query(filter: $"TableName eq '{actionTableName}'");

            if (results.Count() == 0)
            {
                throw new UserInputException($"action table - {actionTableName} not exist, please check whether Date is correct.");
            }

            Console.WriteLine($"action table - {actionTableName} found, retrieving action logs...");

            TableClient tableClient = new TableClient(ConnectionString, actionTableName);
            Pageable<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: "Status eq 'Failed'");

            Dictionary<string, List<FailureRecords>> Records = new Dictionary<string, List<FailureRecords>>();

            //Insert all the failure records as per RunID
            foreach (TableEntity entity in tableEntities)
            {
                //Ignore the failed actions which don't have input and output, mostly they are control action like foreach, until
                if (entity.GetBinary("InputsLinkCompressed") == null && entity.GetBinary("OutputsLinkCompressed") == null)
                {
                    continue;
                }

                string RunID = entity.GetString("FlowRunSequenceId");

                if (!Records.ContainsKey(RunID))
                {
                    Records.Add(RunID, new List<FailureRecords>());
                }

                Records[RunID].Add(new FailureRecords(entity));
            }

            string LogFolder = $"{Directory.GetCurrentDirectory()}/FailureLogs";

            if (!Directory.Exists(LogFolder))
            {
                Directory.CreateDirectory(LogFolder);
            }

            string FilePath = $"{LogFolder}/{LogicAppName}_{WorkflowName}_{Date}_FailureLogs.json";

            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);

                Console.WriteLine($"File already exists, the previous log file has been deleted");
            }

            File.AppendAllText(FilePath, JsonConvert.SerializeObject(Records, Formatting.Indented));
            Console.WriteLine($"Failure log generated, please check the file - {FilePath}");
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

            [JsonIgnore]
            public CommonPayloadStructure InputsLink { get; private set; }
            [JsonIgnore]
            public CommonPayloadStructure OutputsLink { get; private set; }

            public FailureRecords(TableEntity te)
            {
                this.Timestamp = te.GetDateTimeOffset("Timestamp") ?? DateTimeOffset.MinValue;
                this.ActionName = te.GetString("ActionName");
                this.Code = te.GetString("Code");
                this.RepeatItemName = te.GetString("RepeatItemScopeName");
                this.RepeatItemIdenx = te.GetInt32("RepeatItemIndex");

                this.InputContent = DecodeActionPayload(te.GetBinary("InputsLinkCompressed"));
                this.OutputContent = DecodeActionPayload(te.GetBinary("OutputsLinkCompressed"));

                string RawError = DecompressContent(te.GetBinary("Error"));
                this.Error = String.IsNullOrEmpty(RawError) ? null : JsonConvert.DeserializeObject<ActionError>(RawError);
            }
        }
    }
}
