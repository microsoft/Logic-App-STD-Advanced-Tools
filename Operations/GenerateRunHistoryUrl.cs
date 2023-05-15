using Azure.Storage.Files.Shares.Models;
using Azure.Storage.Files.Shares;
using McMaster.Extensions.CommandLineUtils;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using Azure;
using Azure.Data.Tables;
using System.Linq;
using Azure.Data.Tables.Models;
using System.Globalization;
using System.Text.Encodings.Web;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        private static void GenerateRunHistoryUrl(string LogicAppName, string WorkflowName, string Date, string Filter)
        {
            string SubscriptionID = Environment.GetEnvironmentVariable("WEBSITE_OWNER_NAME").Split('+')[0];
            string ResourceGroup = Environment.GetEnvironmentVariable("WEBSITE_RESOURCE_GROUP");
            string Location = Environment.GetEnvironmentVariable("REGION_NAME");

            string Prefix = GenerateWorkflowTablePrefix(LogicAppName, WorkflowName);
            string RunTableName = $"flow{Prefix}runs";

            DateTime MinTimeStamp = DateTime.ParseExact(Date, "yyyyMMdd", CultureInfo.InvariantCulture);
            DateTime MaxTimeStamp = MinTimeStamp.AddDays(1);
            
            TableServiceClient serviceClient = new TableServiceClient(ConnectionString);
            Pageable<TableItem> results = serviceClient.Query(filter: $"TableName eq '{RunTableName}'");

            if (results.Count() == 0)
            {
                throw new UserInputException($"Run history table - {RunTableName} not exist, please check whether Date is correct.");
            }

            Console.WriteLine($"Run history table - {RunTableName} found, retrieving action logs...");

            TableClient tableClient = new TableClient(ConnectionString, RunTableName);
            Pageable<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: $"Status eq 'Failed' and CreatedTime ge datetime'{MinTimeStamp.ToString("yyyy-MM-ddTHH:mm:ssZ")}' and EndTime le datetime'{MaxTimeStamp.ToString("yyyy-MM-ddTHH:mm:ssZ")}'");

            if (tableEntities.Count() == 0)
            {
                throw new UserInputException($"No failure runs detected for workflow {WorkflowName} on {MinTimeStamp.ToString("yyyy-MM-dd")}");
            }

            List<WorkflowRunInfo> Runs = new List<WorkflowRunInfo>();
            foreach (TableEntity entity in tableEntities)
            {
                string RunID = entity.GetString("FlowRunSequenceId");
                string StartTime = entity.GetDateTimeOffset("CreatedTime")?.ToString("yyyy-MM-ddTHH:mm:ssZ");
                string EndTime = entity.GetDateTimeOffset("EndTime")?.ToString("yyyy-MM-ddTHH:mm:ssZ");

                if (string.IsNullOrEmpty(Filter))
                {
                    Runs.Add(new WorkflowRunInfo(SubscriptionID, ResourceGroup, LogicAppName, WorkflowName, RunID, Location, StartTime, EndTime));
                }
                else
                {
                    string ActionTableName = $"flow{Prefix}{Date}t000000zactions";

                    tableClient = new TableClient(ConnectionString, ActionTableName);
                    tableEntities = tableClient.Query<TableEntity>(filter: $"Status eq 'Failed' and FlowRunSequenceId eq '{RunID}'");

                    if (tableEntities.Count() == 0)
                    {
                        continue;
                    }

                    foreach (TableEntity te in tableEntities)
                    {
                        string OutputContent = JsonConvert.SerializeObject(DecodeActionPayload(te.GetBinary("OutputsLinkCompressed")));
                        string RawError = JsonConvert.SerializeObject(DecompressContent(te.GetBinary("Error")));

                        if (OutputContent.Contains(Filter) || RawError.Contains(Filter))
                        {
                            Runs.Add(new WorkflowRunInfo(SubscriptionID, ResourceGroup, LogicAppName, WorkflowName, RunID, Location, StartTime, EndTime));

                            break;
                        }
                    }
                }
            }

            if (Runs.Count == 0)
            {
                throw new UserInputException($"There's no failure run detect for filter: {Filter}");
            }

            string JsonContent = JsonConvert.SerializeObject(Runs, Formatting.Indented);

            string FileName = $"RunHistoryUrl_{LogicAppName}_{WorkflowName}_{Date}.json";
            File.AppendAllText(FileName, JsonContent);

            Console.WriteLine($"Failed run history url generated success, please check file {FileName}");
        }
    }

    public class WorkflowRunInfo
    {
        private string SubscriptionID { get; set; }
        private string ResourceGroup { get; set; }
        private string LogicAppName { get; set; }
        private string WorkflowName { get; set; }
        private string Location { get; set; }
        public string RunID { get; private set; }
        public string StartTime { get; private set; }
        public string EndTime { get; private set; }
        private string ID
        {
            get 
            { 
                return UrlEncoder.Default.Encode($"/subscriptions/{SubscriptionID}/resourcegroups/{ResourceGroup}/providers/microsoft.web/sites/{LogicAppName}/workflows/{WorkflowName}");
            }
        }

        private string ResourceID
        {
            get
            {
                return UrlEncoder.Default.Encode($"/workflows/{WorkflowName}/runs/{RunID}");
            }
        }

        private string Payload = UrlEncoder.Default.Encode("{\"trigger\":{\"name\":\"\"}}");


        public string RunHistoryUrl
        {
            get
            {
                return $"https://portal.azure.com/#view/Microsoft_Azure_EMA/WorkflowMonitorBlade/id/{ID}/location/{Location}/resourceId/{ResourceID}/runProperties~/{Payload}/isReadOnly~/false";
            }
        }

        public WorkflowRunInfo(string SubscriptionID, string ResourceGroup, string LogicAppName, string WorkflowName, string RunID, string Location, string StartTime, string EndTime)
        {
            this.SubscriptionID = SubscriptionID;
            this.ResourceGroup = ResourceGroup;
            this.LogicAppName = LogicAppName;
            this.WorkflowName = WorkflowName;
            this.RunID = RunID;
            this.StartTime = StartTime;
            this.EndTime = EndTime;
            this.Location = UrlEncoder.Default.Encode(Location);
        }
    }
}
