using Azure.Data.Tables;
using System.Collections.Generic;
using System.Linq;

namespace LogicAppAdvancedTool.Operations
{
    public static class ListWorkflows
    {
        public static void Run()
        {
            List<TableEntity> entities = TableOperations.QueryMainTable(null, select: new string[] { "FlowName", "ChangedTime", "Kind", "FlowId" })
                                .GroupBy(t => t.GetString("FlowName"))
                                .Select(g => g.OrderByDescending(
                                    x => x.GetDateTimeOffset("ChangedTime"))
                                    .FirstOrDefault())
                                .ToList();

            if (entities.Count == 0)
            {
                throw new UserInputException("No workflow found.");
            }

            ConsoleTable consoleTable = new ConsoleTable(new List<string>() { "Workflow Name", "Last Updated (UTC)" });

            foreach (TableEntity entity in entities)
            {
                string flowName = entity.GetString("FlowName");
                string changedTime = entity.GetDateTimeOffset("ChangedTime")?.ToString("yyyy-MM-ddTHH:mm:ssZ");
                string flowId = entity.GetString("FlowId");

                consoleTable.AddRow(new List<string>() { flowName, flowId, changedTime });
            }

            consoleTable.Print();
        }
    }
}