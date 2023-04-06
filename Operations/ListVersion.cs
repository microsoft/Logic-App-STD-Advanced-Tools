using Azure;
using Azure.Data.Tables;
using System;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        private static void ListVersions(string LogicAppName, string WorkflowName)
        {
            string TableName = GetMainTableName(LogicAppName);

            if (string.IsNullOrEmpty(TableName))
            {
                return;
            }

            TableClient tableClient = new TableClient(ConnectionString, TableName);
            Pageable<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: $"FlowName eq '{WorkflowName}'");

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