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
        private static void ListWorkflowID(string workflowName)
        {
            List<TableEntity> entities = TableOperations.QueryMainTable($"FlowName eq '{workflowName}'", select: new string[] { "FlowName", "FlowId", "ChangedTime", "Kind" })
                                            .GroupBy(t => t.GetString("FlowId"))
                                            .Select(g => g.OrderByDescending( 
                                                x => x.GetDateTimeOffset("ChangedTime"))
                                                .FirstOrDefault())
                                            .ToList();

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