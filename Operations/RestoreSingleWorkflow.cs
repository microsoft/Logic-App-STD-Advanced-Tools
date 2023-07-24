using Azure.Data.Tables;
using System;
using System.IO;
using System.Linq;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        private static void RestoreSingleWorkflow(string workflowName)
        {
            TableEntity entity = TableOperations.QueryMainTable($"FlowName eq '{workflowName}'", select: new string[] { "FlowName", "ChangedTime", "DefinitionCompressed", "Kind" })
                                        .GroupBy(t => t.GetString("FlowName"))
                                        .Select(g => g.OrderByDescending(
                                            x => x.GetDateTimeOffset("ChangedTime"))
                                            .FirstOrDefault())
                                        .ToList()
                                        .FirstOrDefault();

            if (entity == null)
            {
                throw new UserInputException($"{workflowName} cannot be found in storage table, please check whether workflow is correct.");
            }

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
