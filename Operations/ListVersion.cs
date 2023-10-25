using Azure.Data.Tables;
using System.Collections.Generic;
using System.Linq;

namespace LogicAppAdvancedTool.Operations
{
    public static class ListVersions
    {
        public static void Run(string workflowName)
        {
            List<TableEntity> tableEntities = TableOperations.QueryMainTable($"FlowName eq '{workflowName}'", new string[] { "RowKey", "FlowId", "FlowSequenceId", "FlowUpdatedTime" })
                                                .Where(t => t.GetString("RowKey").Contains("FLOWVERSION"))
                                                .OrderByDescending(t => t.GetDateTimeOffset("FlowUpdatedTime"))
                                                .ToList();

            if (tableEntities.Count == 0)
            {
                throw new UserInputException($"{workflowName} cannot be found in storage table, please check whether workflow is correct.");
            }

            ConsoleTable consoleTable = new ConsoleTable("Workflow ID", "Version ID", "Updated Time (UTC)");

            foreach (TableEntity entity in tableEntities)
            {
                string flowID = entity.GetString("FlowId");
                string version = entity.GetString("FlowSequenceId");
                string updateTime = entity.GetDateTimeOffset("FlowUpdatedTime")?.ToString("yyyy-MM-ddTHH:mm:ssZ");

                consoleTable.AddRow(flowID, version, updateTime);
            }

            consoleTable.Print();
        }
    }
}