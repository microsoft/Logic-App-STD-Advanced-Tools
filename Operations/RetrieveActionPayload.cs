using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Globalization;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        private static void RetrieveActionPayload(string workflowName, string date, string actionName)
        {
            List<TableEntity> histories = new List<TableEntity>();

            DateTime minTimeStamp = DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture);
            DateTime maxTimeStamp = minTimeStamp.AddDays(1);

            string triggerQuery = $"CreatedTime ge datetime'{minTimeStamp.ToString("yyyy-MM-ddTHH:mm:ssZ")}' and CreatedTime le datetime'{maxTimeStamp.ToString("yyyy-MM-ddTHH:mm:ssZ")}' and TriggerName eq '{actionName}'";
            histories.AddRange(TableOperations.QueryHistoryTable(workflowName, triggerQuery));

            string actionQuery = $"CreatedTime ge datetime'{minTimeStamp.ToString("yyyy-MM-ddTHH:mm:ssZ")}' and CreatedTime le datetime'{maxTimeStamp.ToString("yyyy-MM-ddTHH:mm:ssZ")}' and ActionName eq '{actionName}'";
            histories.AddRange(TableOperations.QueryActionTable(workflowName, date, actionQuery));

            if (histories.Count == 0)
            {
                throw new ExpectedException("No records found, please verify the options you provided.");
            }

            List<ActionPayload> payloads = new List<ActionPayload>();
            foreach (TableEntity te in histories)
            {
                payloads.Add(new ActionPayload(te));
            }

            string content = JsonConvert.SerializeObject(payloads, Formatting.Indented);
            string logPath = $"{workflowName}_{date}_{actionName}.json";

            if (File.Exists(logPath))
            { 
                Console.WriteLine($"File {logPath} already exist, existing file will be overwritten.");
                File.Delete(logPath);
            }

            File.WriteAllText(logPath, content);
            Console.WriteLine($"Retrieved payload, please check {logPath} for detail information.");
        }
    }
}
