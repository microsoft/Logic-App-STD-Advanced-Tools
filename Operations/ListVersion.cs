using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        private static void ListVersions(string logicAppName, string workflowName)
        {
            string tableName = GetMainTableName(logicAppName);

            TableClient tableClient = new TableClient(AppSettings.ConnectionString, tableName);
            List<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: $"FlowName eq '{workflowName}'").ToList();

            if (tableEntities.Count == 0)
            {
                throw new UserInputException($"{workflowName} cannot be found in storage table, please check whether workflow is correct.");
            }

            ConsoleTable consoleTable = new ConsoleTable("Version ID", "Updated Time (UTC)");

            foreach (TableEntity entity in tableEntities)
            {
                string rowKey = entity.GetString("RowKey");

                if (rowKey.Contains("FLOWVERSION"))
                {
                    string version = entity.GetString("FlowSequenceId");
                    string updateTime = entity.GetDateTimeOffset("FlowUpdatedTime")?.ToString("yyyy-MM-ddTHH:mm:ssZ");

                    consoleTable.AddRow(version, updateTime);
                }
            }

            consoleTable.Print();
        }
    }
}