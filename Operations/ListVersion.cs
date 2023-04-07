using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        private static void ListVersions(string LogicAppName, string WorkflowName)
        {
            string TableName = GetMainTableName(LogicAppName);

            TableClient tableClient = new TableClient(ConnectionString, TableName);
            List<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: $"FlowName eq '{WorkflowName}'").ToList();

            if (tableEntities.Count == 0)
            {
                throw new UserInputException($"{WorkflowName} cannot be found in storage table, please check whether workflow is correct.");
            }

            ConsoleTable consoleTable = new ConsoleTable("Version ID", "Updated Time (UTC)");

            foreach (TableEntity entity in tableEntities)
            {
                string RowKey = entity.GetString("RowKey");

                if (RowKey.Contains("FLOWVERSION"))
                {
                    string Version = entity.GetString("FlowSequenceId");
                    string UpdateTime = entity.GetDateTimeOffset("FlowUpdatedTime")?.ToString("yyyy-MM-ddTHH:mm:ssZ");

                    consoleTable.AddRow(Version, UpdateTime);
                }
            }

            consoleTable.Print();
        }
    }
}