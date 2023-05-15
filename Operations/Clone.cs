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
        /// <param name="SourceName"></param>
        /// <param name="TargetName"></param>
        /// <param name="Version"></param>
        private static void Clone(string LogicAppName, string SourceName, string TargetName, string Version)
        {
            string TableName = GetMainTableName(LogicAppName);

            TableClient tableClient = new TableClient(connectionString, TableName);
            Pageable<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: $"FlowName eq '{SourceName}'");

            string Content = String.Empty;

            string Identity = string.IsNullOrEmpty(Version) ? "FLOWIDENTIFIER" : Version;

            foreach (TableEntity entity in tableEntities)
            {
                string RowKey = entity.GetString("RowKey");

                if (RowKey.Contains(Identity))
                {
                    byte[] DefinitionCompressed = entity.GetBinary("DefinitionCompressed");
                    string Kind = entity.GetString("Kind");
                    string DecompressedDefinition = DecompressContent(DefinitionCompressed);

                    string OutputContent = $"{{\"definition\": {DecompressedDefinition},\"kind\": \"{Kind}\"}}";
                    string ClonePath = $"C:/home/site/wwwroot/{TargetName}";

                    if (Directory.Exists(ClonePath))
                    {
                        throw new UserInputException("Workflow already exists, workflow will not be cloned. Please use another target name.");
                    }

                    Directory.CreateDirectory(ClonePath);
                    File.WriteAllText($"{ClonePath}/workflow.json", OutputContent);

                    break;
                }
            }

            Console.WriteLine("Clone finished, please refresh workflow page");
        }
    }
}
