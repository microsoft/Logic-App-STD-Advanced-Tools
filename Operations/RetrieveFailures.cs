using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Text;

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

                return;
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

            Console.WriteLine($"action table - {actionTableName} found, retrieving action logs...");

            tableClient = new TableClient(ConnectionString, actionTableName);
            tableEntities = tableClient.Query<TableEntity>(filter: "Status eq 'Failed'");

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

                this.InputContent = DecodePayload(te.GetBinary("InputsLinkCompressed"));
                this.OutputContent = DecodePayload(te.GetBinary("OutputsLinkCompressed"));

                string RawError = DecompressContent(te.GetBinary("Error"));
                this.Error = String.IsNullOrEmpty(RawError) ? null : JsonConvert.DeserializeObject<ActionError>(RawError);
            }

            private dynamic DecodePayload(byte[] BinaryContent)
            {
                string RawContent = DecompressContent(BinaryContent);

                if (RawContent == null)
                {
                    return null;
                }

                //Recently there are 2 different JSON schema for output payload, try connector schema first
                ConnectorPayloadStructure ConnectorPayload = JsonConvert.DeserializeObject<ConnectorPayloadStructure>(RawContent);

                dynamic Output = null;

                if (ConnectorPayload.nestedContentLinks != null)
                {
                    string inlineContent = ConnectorPayload.nestedContentLinks.body.inlinedContent;
                    Output = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(Convert.FromBase64String(inlineContent)));
                    return Output;
                }

                CommonPayloadStructure Payload = JsonConvert.DeserializeObject<CommonPayloadStructure>(RawContent);
                Output = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(Convert.FromBase64String(Payload.inlinedContent)));

                return Output;
            }
        }

        #region payload structure for connector action (built-in and Azure)
        public class ConnectorPayloadStructure
        {
            public NestedContentLinks nestedContentLinks { get; set; }
        }

        public class NestedContentLinks
        { 
            public CommonPayloadStructure body { get; set; }
        }
        #endregion


        public class CommonPayloadStructure
        {
            public string inlinedContent { get; set; }
            public string contentVersion { get; set; }
            public int contentSize { get; set; }
            public ContentHash contentHash { get; set; }
        }

        public class ContentHash
        {
            public string algorithm { get; set; }
            public string value { get; set; }
        }

        public class ActionError
        { 
            public string code { get; set; }
            public string message { get; set; }
        }
    }
}
