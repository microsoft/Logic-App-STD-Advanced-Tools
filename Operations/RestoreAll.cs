using Azure;
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
        private static void RestoreAll(string LogicAppName)
        {
            string TableName = GetMainTableName(LogicAppName);

            if (String.IsNullOrEmpty(TableName))
            {
                return;
            }

            string ConfirmationMessage = "WARNING!!!\r\nThis operation will restore all the deleted workflows, if there's any invalid workflows, it might cause unexpected behavior on Logic App runtime.\r\nBe cautuion if you are running this command in PROD environment\r\nPlease input for confirmation:";
            if (!Prompt.GetYesNo(ConfirmationMessage, false))
            {
                Console.WriteLine("Operation Cancelled");

                return;
            }

            string BackupPath = BackupCurrentSite();
            Console.WriteLine($"Backup current workflows, you can find in path: {BackupPath}");

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
