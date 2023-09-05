using Azure.Data.Tables;
using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        private static void RestoreAll()
        {
            string confirmationMessage = "WARNING!!!\r\nThis operation will restore all the deleted workflows, if there's any invalid workflows, it might cause unexpected behavior on Logic App runtime.\r\nBe cautuion if you are running this command in PROD environment\r\nPlease input for confirmation:";
            if (!Prompt.GetYesNo(confirmationMessage, false, ConsoleColor.Red))
            {
                throw new UserCanceledException("Operation Cancelled");
            }

            string backupPath = BackupCurrentSite();
            Console.WriteLine($"Backup current workflows, you can find in path: {backupPath}");

            List<TableEntity> entities = TableOperations.QueryMainTable(null, select: new string[] { "FlowName", "ChangedTime", "DefinitionCompressed", "Kind" })
                    .GroupBy(t => t.GetString("FlowName"))
                    .Select(g => g.OrderByDescending(
                        x => x.GetDateTimeOffset("ChangedTime"))
                        .FirstOrDefault())
                    .ToList();

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