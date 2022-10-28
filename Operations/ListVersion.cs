using Azure;
using Azure.Data.Tables;
using System;

namespace LAVersionReverter
{
    partial class Program
    {
        private static void ListVersions(string LogicAppName, string ConnectionString, string WorkflowName)
        {
            string TableName = GetMainTableName(LogicAppName, ConnectionString);

            TableClient tableClient = new TableClient(ConnectionString, TableName);
            Pageable<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: $"FlowName eq '{WorkflowName}'");

            foreach (TableEntity entity in tableEntities)
            {
                string RowKey = entity.GetString("RowKey");

                if (RowKey.Contains("FLOWVERSION"))
                {
                    string Version = entity.GetString("FlowSequenceId");
                    DateTimeOffset? UpdateTime = entity.GetDateTimeOffset("FlowUpdatedTime");

                    Console.WriteLine($"Version ID:{Version}    UpdateTime:{UpdateTime}");
                }
            }
        }
    }
}