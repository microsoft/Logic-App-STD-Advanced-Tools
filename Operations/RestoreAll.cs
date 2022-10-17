using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LAVersionReverter
{
    partial class Program
    {
        private static void RestoreAll(string LogicAppName, string ConnectionString)
        {
            string TableName = GetMainTableName(LogicAppName, ConnectionString);

            TableClient tableClient = new TableClient(ConnectionString, TableName);
            Pageable<TableEntity> tableEntities = tableClient.Query<TableEntity>(select: new string[] { "FlowName", "ChangedTime", "DefinitionCompressed", "Kind" });

            List<TableEntity> entities = (from n in tableEntities
                                          group n by n.GetString("FlowName") into g
                                          select g.OrderByDescending(x => x.GetDateTimeOffset("ChangedTime")).FirstOrDefault()).ToList();

            foreach (TableEntity entity in entities)
            {
                string flowName = entity.GetString("FlowName");
                byte[] definitionCompressed = entity.GetBinary("DefinitionCompressed");
                string kind = entity.GetString("Kind");
                string decompressedDefinition = DecompressContent(definitionCompressed);

                string outputContent = $"{{\"definition\": {decompressedDefinition},\"kind\": \"{kind}\"}}";

                string workflowPath = $"C:/home/site/wwwroot/{flowName}";
                if (!Directory.Exists(workflowPath))
                {
                    Directory.CreateDirectory(workflowPath);
                }

                string definitionPath = $"{workflowPath}/workflow.json";

                File.WriteAllText(definitionPath, outputContent);

                Console.WriteLine($"Workflow: {flowName} restored successfully.");
            }

            Console.WriteLine("Restored all the workflows which found in Storage Table. Please refresh workflow page.");
        }
    }
}
