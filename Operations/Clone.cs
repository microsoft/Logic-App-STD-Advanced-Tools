using Azure;
using Azure.Data.Tables;
using System;
using System.IO;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        /// <summary>
        /// Clone a workflow definition (also can be a old version) to a new one
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <param name="sourceName"></param>
        /// <param name="TargetName"></param>
        /// <param name="version"></param>
        private static void Clone(string logicAppName, string sourceName, string targetName, string version)
        {
            string tableName = GetMainTableName(logicAppName);

            TableClient tableClient = new TableClient(AppSettings.ConnectionString, tableName);
            Pageable<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: $"FlowName eq '{sourceName}'");

            string identity = string.IsNullOrEmpty(version) ? "FLOWIDENTIFIER" : version;

            foreach (TableEntity entity in tableEntities)
            {
                string rowKey = entity.GetString("RowKey");

                if (rowKey.Contains(identity))
                {
                    byte[] definitionCompressed = entity.GetBinary("DefinitionCompressed");
                    string kind = entity.GetString("Kind");
                    string decompressedDefinition = DecompressContent(definitionCompressed);

                    string outputContent = $"{{\"definition\": {decompressedDefinition},\"kind\": \"{kind}\"}}";
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

            Console.WriteLine("Clone finished, please refresh workflow page");
        }
    }
}
