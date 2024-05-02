using Azure.Data.Tables;
using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LogicAppAdvancedTool.Operations
{
    public static class RestoreAll
    {
        public static void Run()
        {
            string confirmationMessage = "WARNING!!!\r\nThis operation will restore all the deleted workflows, if there's any invalid workflows, it might cause unexpected behavior on Logic App runtime.\r\nBe cautuion if you are running this command in PROD environment\r\nPlease input for confirmation:";
            if (!Prompt.GetYesNo(confirmationMessage, false, ConsoleColor.Red))
            {
                throw new UserCanceledException("Operation Cancelled");
            }

            string backupPath = CommonOperations.BackupCurrentSite();
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
                string workflowPath = $"{AppSettings.RootFolder}\\{flowName}";
                
                CommonOperations.SaveDefinition(workflowPath, "workflow.json", entity);

                Console.WriteLine($"Workflow: {flowName} restored successfully.");
            }

            Console.WriteLine("Restored all the workflows which found in Storage Table. Please refresh workflow page.");
        }
    }
}