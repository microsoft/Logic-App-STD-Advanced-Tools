using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using LogicAppAdvancedTool.Structures;

namespace LogicAppAdvancedTool
{
    public static class RetrieveFailures
    {
        public static void RetrieveFailuresByDate(string workflowName, string date)
        {
            List<TableEntity> tableEntities = TableOperations.QueryActionTable(workflowName, date, "Status eq 'Failed'");

            string fileName = $"{AppSettings.LogicAppName}_{workflowName}_{date}_FailureLogs.json";

            SaveFailureLogs(tableEntities, fileName);
        }

        public static void RetrieveFailuresByRun(string workflowName, string runID)
        {
            TableEntity runEntity = TableOperations.QueryRunTable(workflowName, $"FlowRunSequenceId eq '{runID}'").First();

            if (runEntity == null)
            {
                throw new UserInputException($"Cannot find workflow run with run id: {runID} of workflow: {workflowName}, please check your input.");
            }

            Console.WriteLine($"Workflow run id found in run history table. Retrieving failure actions.");

            string date = runEntity.GetDateTimeOffset("CreatedTime")?.ToString("yyyyMMdd");
            List<TableEntity> tableEntities = TableOperations.QueryActionTable(workflowName, date, $"Status eq 'Failed' and FlowRunSequenceId eq '{runID}'");

            string fileName = $"{AppSettings.LogicAppName}_{workflowName}_{runID}_FailureLogs.json";

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

                if (failureRecords.Error != null && failureRecords.Error.message.Contains("An action failed. No dependent actions succeeded."))
                {
                    continue;       //exclude actions (eg:foreach, until) which failed due to inner actions.
                }

                if (!records.ContainsKey(runID))
                {
                    records.Add(runID, new List<HistoryRecords>());
                }

                records[runID].Add(failureRecords);
            }

            if (File.Exists(fileName))
            {
                File.Delete(fileName);

                Console.WriteLine($"File already exists, the previous log file has been deleted");
            }

            File.AppendAllText(fileName, JsonConvert.SerializeObject(records, Formatting.Indented));
            Console.WriteLine($"Failure log generated, please check the file - {fileName}");
        }
    }
}
