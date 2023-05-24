using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        private static void ListWorkflowID(string logicAppName, string workflowName)
        {
            string tableName = GetMainTableName(logicAppName);

            TableClient tableClient = new TableClient(AppSettings.ConnectionString, tableName);
            Pageable<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: $"FlowName eq '{workflowName}'", select: new string[] { "FlowName", "FlowId", "ChangedTime", "Kind" });

            List<TableEntity> entities = (from n in tableEntities
                                          group n by n.GetString("FlowId") into g
                                          select g.OrderByDescending(x => x.GetDateTimeOffset("ChangedTime")).FirstOrDefault()).ToList();

            if (entities.Count == 0)
            {
                throw new UserInputException("No workflow found.");
            }

            ConsoleTable consoleTable = new ConsoleTable("Workflow Name", "Flow ID", "Last Updated (UTC)", "Kind");

            foreach (TableEntity entity in entities)
            {
                string flowName = entity.GetString("FlowName");
                string changedTime = entity.GetDateTimeOffset("ChangedTime")?.ToString("yyyy-MM-ddTHH:mm:ssZ");
                string flowID = entity.GetString("FlowId");
                string kind = entity.GetString("Kind");

                consoleTable.AddRow(flowName, flowID, changedTime, kind);
            }

            consoleTable.Print();
        }
    }
}