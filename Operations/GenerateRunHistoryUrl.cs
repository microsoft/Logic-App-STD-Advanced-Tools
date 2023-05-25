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
        private static void GenerateRunHistoryUrl(string logicAppName, string workflowName, string date, string filter)
        {
            string tablePrefix = GenerateWorkflowTablePrefix(logicAppName, workflowName);
            string runTableName = $"flow{tablePrefix}runs";

            DateTime minTimeStamp = DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture);
            DateTime maxTimeStamp = minTimeStamp.AddDays(1);
            
            TableServiceClient serviceClient = new TableServiceClient(AppSettings.ConnectionString);
            Pageable<TableItem> results = serviceClient.Query(filter: $"TableName eq '{runTableName}'");

            if (results.Count() == 0)
            {
                throw new UserInputException($"Run history table - {runTableName} not exist, please check whether Date is correct.");
            }

            Console.WriteLine($"Run history table - {runTableName} found, retrieving action logs...");

            TableClient tableClient = new TableClient(AppSettings.ConnectionString, runTableName);
            Pageable<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: $"Status eq 'Failed' and CreatedTime ge datetime'{minTimeStamp.ToString("yyyy-MM-ddTHH:mm:ssZ")}' and EndTime le datetime'{maxTimeStamp.ToString("yyyy-MM-ddTHH:mm:ssZ")}'");

            if (tableEntities.Count() == 0)
            {
                throw new UserInputException($"No failure runs detected for workflow {workflowName} on {minTimeStamp.ToString("yyyy-MM-dd")}");
            }

            List<WorkflowRunInfo> runs = new List<WorkflowRunInfo>();
            foreach (TableEntity entity in tableEntities)
            {
                string runID = entity.GetString("FlowRunSequenceId");
                string startTime = entity.GetDateTimeOffset("CreatedTime")?.ToString("yyyy-MM-ddTHH:mm:ssZ");
                string endTime = entity.GetDateTimeOffset("EndTime")?.ToString("yyyy-MM-ddTHH:mm:ssZ");

                if (string.IsNullOrEmpty(filter))
                {
                    runs.Add(new WorkflowRunInfo(logicAppName, workflowName, runID, startTime, endTime));
                }
                else
                {
                    string actionTableName = $"flow{tablePrefix}{date}t000000zactions";

                    tableClient = new TableClient(AppSettings.ConnectionString, actionTableName);
                    tableEntities = tableClient.Query<TableEntity>(filter: $"Status eq 'Failed' and FlowRunSequenceId eq '{runID}'");

                    if (tableEntities.Count() == 0)
                    {
                        continue;
                    }

                    foreach (TableEntity te in tableEntities)
                    {
                        //TODO: improve DecodeActionPayload method for directly return string
                        string outputContent = DecodeActionPayloadAsString(te.GetBinary("OutputsLinkCompressed"));
                        string rawError = DecompressContent(te.GetBinary("Error"));

                        if (outputContent.Contains(filter) || rawError.Contains(filter))
                        {
                            runs.Add(new WorkflowRunInfo(logicAppName, workflowName, runID, startTime, endTime));

                            break;
                        }
                    }
                }
            }

            if (runs.Count == 0)
            {
                throw new UserInputException($"There's no failure run detect for filter: {filter}");
            }

            string jsonContent = JsonConvert.SerializeObject(runs, Formatting.Indented);

            string fileName = $"{logicAppName}_{workflowName}_{date}_RunHistoryUrl.json";

            if (File.Exists(fileName))
            {
                File.Delete(fileName);

                Console.WriteLine($"File already exists, the previous log file has been deleted");
            }

            File.AppendAllText(fileName, jsonContent);

            Console.WriteLine($"Failed run history url generated success, please check file {fileName}");
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

        public WorkflowRunInfo(string logicAppName, string workflowName, string runID, string startTime, string endTime)
        {
            this.SubscriptionID = AppSettings.SubscriptionID;
            this.ResourceGroup = AppSettings.ResourceGroup;
            this.LogicAppName = logicAppName;
            this.WorkflowName = workflowName;
            this.RunID = runID;
            this.StartTime = startTime;
            this.EndTime = endTime;
            this.Location = UrlEncoder.Default.Encode(AppSettings.Region);
        }
    }
}
