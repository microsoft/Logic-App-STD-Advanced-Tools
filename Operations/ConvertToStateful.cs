using Azure;
using Azure.Data.Tables;
using System;
using System.IO;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        private static void ConvertToStateful(string logicAppName, string sourceName, string targetName)
        {
            string tableName = GetMainTableName(logicAppName);

            TableClient tableClient = new TableClient(AppSettings.ConnectionString, tableName);
            Pageable<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: $"FlowName eq '{sourceName}'");

            string identity = "FLOWIDENTIFIER";

            foreach (TableEntity entity in tableEntities)
            {
                string rowKey = entity.GetString("RowKey");

                if (rowKey.Contains(identity))
                {
                    byte[] definitionCompressed = entity.GetBinary("DefinitionCompressed");
                    string decompressedDefinition = DecompressContent(definitionCompressed);

                    string outputContent = $"{{\"definition\": {decompressedDefinition},\"kind\": \"Stateful\"}}";
                    string clonePath = $"C:/home/site/wwwroot/{targetName}";

                    if (Directory.Exists(clonePath))
                    {
                        throw new UserInputException("Workflow already exists, workflow will not be cloned. Please use another target name.");
                    }

                    Directory.CreateDirectory(clonePath);
                    File.WriteAllText($"{clonePath}/workflow.json", outputContent);

                    break;
                }
            }

            Console.WriteLine("Convert finished, please refresh workflow page");
        }
    }
}
