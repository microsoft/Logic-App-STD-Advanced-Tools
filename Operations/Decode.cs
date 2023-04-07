using Azure;
using Azure.Data.Tables;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        private static void Decode(string LogicAppName, string ConnectionString, string WorkflowName, string Version)
        {
            string TableName = GetMainTableName(LogicAppName);

            TableClient tableClient = new TableClient(ConnectionString, TableName);
            List<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: $"FlowName eq '{WorkflowName}' and FlowSequenceId eq '{Version}'").ToList();

            if (tableEntities.Count<TableEntity>() == 0)
            {
                throw new UserInputException($"{WorkflowName} with version {Version} cannot be found in storage table, pleaase check your input.");
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
