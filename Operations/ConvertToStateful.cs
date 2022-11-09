using Azure;
using Azure.Data.Tables;
using System;
using System.IO;

namespace LAVersionReverter
{
    partial class Program
    {
        private static void ConvertToStateful(string LogicAppName, string ConnectionString, string SourceName, string TargetName)
        {
            string TableName = GetMainTableName(LogicAppName, ConnectionString);

            TableClient tableClient = new TableClient(ConnectionString, TableName);
            Pageable<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: $"FlowName eq '{SourceName}'");

            string Content = String.Empty;

            string Identity = "FLOWIDENTIFIER";

            foreach (TableEntity entity in tableEntities)
            {
                string RowKey = entity.GetString("RowKey");

                if (RowKey.Contains(Identity))
                {
                    byte[] DefinitionCompressed = entity.GetBinary("DefinitionCompressed");
                    string DecompressedDefinition = DecompressContent(DefinitionCompressed);

                    string OutputContent = $"{{\"definition\": {DecompressedDefinition},\"kind\": \"Stateful\"}}";
                    string ClonePath = $"C:/home/site/wwwroot/{TargetName}";

                    if (Directory.Exists(ClonePath))
                    {
                        Console.WriteLine("Workflow already exists, workflow will not be cloned. Please use another target name.");
                    }

                    Directory.CreateDirectory(ClonePath);
                    File.WriteAllText($"{ClonePath}/workflow.json", OutputContent);

                    break;
                }
            }

            Console.WriteLine("Convert finished, please refresh workflow page");
        }
    }
}
