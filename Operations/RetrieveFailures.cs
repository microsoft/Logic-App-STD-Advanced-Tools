using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LAVersionReverter
{
    partial class Program
    {
        private static void RetrieveFailures(string LogicAppName, string WorkflowName, string ConnectionString, string Date)
        {
            
            string mainTableName = GetMainTableName(LogicAppName, ConnectionString);

            TableClient tableClient = new TableClient(ConnectionString, mainTableName);
            Pageable<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: $"FlowName eq '{WorkflowName}'");

            if (tableEntities.Count() == 0)
            {
                Console.WriteLine("No workflow found in table, please double check the workflow name");
            }

            string logicAppPrefix = StoragePrefixGenerator.Generate(LogicAppName.ToLower());

            string workflowID = tableEntities.First<TableEntity>().GetString("FlowId");
            string workflowPrefix = StoragePrefixGenerator.Generate(workflowID.ToLower());

            string actionTableName = $"flow{logicAppPrefix}{workflowPrefix}{Date}t000000zactions";

            //Double check whether the action table exists
            TableServiceClient serviceClient = new TableServiceClient(ConnectionString);
            Pageable<TableItem> results = serviceClient.Query(filter: $"TableName eq '{actionTableName}'");

            if (results.Count() == 0)
            {
                Console.WriteLine($"action table - {actionTableName} not exist, please double check the parameters.");

                return;
            }

            tableClient = new TableClient(ConnectionString, actionTableName);
            tableEntities = tableClient.Query<TableEntity>(filter: "Status eq 'Failed'");

            Dictionary<string, List<FailureRecords>> Records = new Dictionary<string, List<FailureRecords>>();

            //Insert all the failure records as per RunID
            foreach (TableEntity entity in tableEntities)
            {
                string RunID = entity.GetString("FlowRunSequenceId");

                if (!Records.ContainsKey(RunID))
                {
                    Records.Add(RunID, new List<FailureRecords>());
                }

                Records[RunID].Add(new FailureRecords(entity));
            }
        }

        public class FailureRecords
        {
            public DateTimeOffset Timestamp { get; private set; }
            public string ActionName { get; private set; }
            public string Code { get; private set; }
            public string Input { get; private set; }
            public string Output { get; private set; }
            public string Error { get; private set; }
            public string RepeatItemName { get; private set; }
            public int? RepeatItemIdenx { get; private set; }

            public FailureRecords(TableEntity te)
            {
                this.Timestamp = te.GetDateTimeOffset("Timestamp") ?? DateTimeOffset.MinValue;
                this.ActionName = te.GetString("ActionName");
                this.Code = te.GetString("Code");
                this.Input = DecompressContent(te.GetBinary("InputsLinkCompressed"));
                this.Output = DecompressContent(te.GetBinary("OutputsLinkCompressed"));
                this.Error = DecompressContent(te.GetBinary("Error"));
                this.RepeatItemName = te.GetString("RepeatItemName");
                this.RepeatItemIdenx = te.GetInt32("RepeatItemIdenx");
            }

        }
    }
}
