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
        private static void ListWorkflows(string logicAppName)
        {
            string tableName = GetMainTableName(logicAppName);

            TableClient tableClient = new TableClient(ConnectionString, tableName);
            Pageable<TableEntity> tableEntities = tableClient.Query<TableEntity>(select: new string[] { "FlowName", "ChangedTime", "DefinitionCompressed", "Kind" });

            List<TableEntity> entities = (from n in tableEntities
                                          group n by n.GetString("FlowName") into g
                                          select g.OrderByDescending(x => x.GetDateTimeOffset("ChangedTime")).FirstOrDefault()).ToList();

            if (entities.Count == 0)
            {
                throw new UserInputException("No workflow found.");
            }

            ConsoleTable consoleTable = new ConsoleTable("Workflow Name", "Last Updated (UTC)");

            foreach (TableEntity entity in entities)
            {
                string flowName = entity.GetString("FlowName");
                string changedTime = entity.GetDateTimeOffset("ChangedTime")?.ToString("yyyy-MM-ddTHH:mm:ssZ");

                consoleTable.AddRow(flowName, changedTime);
            }

            consoleTable.Print();
        }
    }
}
