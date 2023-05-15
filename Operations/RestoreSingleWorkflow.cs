using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        private static void RestoreSingleWorkflow(string LogicAppName, string WorkflowName)
        {
            string TableName = GetMainTableName(LogicAppName);

            TableClient tableClient = new TableClient(connectionString, TableName);
            Pageable<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: $"FlowName eq '{WorkflowName}'", select: new string[] { "FlowName", "ChangedTime", "DefinitionCompressed", "Kind" });

            List<TableEntity> entities = (from n in tableEntities
                                          group n by n.GetString("FlowName") into g
                                          select g.OrderByDescending(x => x.GetDateTimeOffset("ChangedTime")).FirstOrDefault()).ToList();

            if (entities.Count == 0)
            {
                throw new UserInputException($"{WorkflowName} cannot be found in storage table, please check whether workflow is correct.");
            }

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
        }
    }
}
