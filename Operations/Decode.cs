using Azure;
using Azure.Data.Tables;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        private static void Decode(string LogicAppName, string ConnectionString, string WorkflowName, string Version)
        {
            string TableName = GetMainTableName(LogicAppName, ConnectionString);

            TableClient tableClient = new TableClient(ConnectionString, TableName);
            Pageable<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: $"FlowName eq '{WorkflowName}' and FlowSequenceId eq '{Version}'");

            if (tableEntities.Count<TableEntity>() == 0)
            {
                Console.WriteLine("No Record Found! Please check the Workflow name and the Version(FlowSequenceId)");
                return;
            }

            string Content = String.Empty;

            foreach (TableEntity entity in tableEntities)
            {
                byte[] DefinitionCompressed = entity.GetBinary("DefinitionCompressed");
                string DecompressedDefinition = DecompressContent(DefinitionCompressed);

                dynamic JsonObject = JsonConvert.DeserializeObject(DecompressedDefinition);
                string FormattedContent = JsonConvert.SerializeObject(JsonObject, Formatting.Indented);

                Console.Write(FormattedContent);

                break;
            }
        }
    }
}
