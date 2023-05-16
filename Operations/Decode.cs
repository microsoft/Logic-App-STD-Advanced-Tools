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
        private static void Decode(string logicAppName, string workflowName, string version)
        {
            string tableName = GetMainTableName(logicAppName);

            TableClient tableClient = new TableClient(AppSettings.ConnectionString, tableName);
            List<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: $"FlowName eq '{workflowName}' and FlowSequenceId eq '{version}'").ToList();

            if (tableEntities.Count<TableEntity>() == 0)
            {
                throw new UserInputException($"{workflowName} with version {version} cannot be found in storage table, pleaase check your input.");
            }

            foreach (TableEntity entity in tableEntities)
            {
                byte[] definitionCompressed = entity.GetBinary("DefinitionCompressed");
                string decompressedDefinition = DecompressContent(definitionCompressed);

                dynamic jsonObject = JsonConvert.DeserializeObject(decompressedDefinition);
                string formattedContent = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);

                Console.Write(formattedContent);

                break;
            }
        }
    }
}
