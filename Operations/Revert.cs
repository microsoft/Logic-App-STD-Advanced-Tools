using McMaster.Extensions.CommandLineUtils;
using System;
using System.IO;

namespace LAVersionReverter
{
    partial class Program
    {
        private static void RevertVersion(string WorkflowName, string Version)
        {
            string BackupFilePath = $"{Directory.GetCurrentDirectory()}/Backup/{WorkflowName}";

            if (!Directory.Exists(BackupFilePath))
            {
                Console.WriteLine("Workflow name not found, please double check the provided workflow name.");

                return;
            }

            string[] Files = Directory.GetFiles(BackupFilePath, $"*{Version}.json");

            if (Files == null || Files.Length == 0)
            {
                Console.WriteLine("No backup file found, please check the name and version of workflow");

                return;
            }

            string ConfirmationMessage = $"CAUTION!!!\r\nThe current workflow: {WorkflowName} will be overwrite\r\n\r\nPlease input for confirmation:";
            if (!Prompt.GetYesNo(ConfirmationMessage, false))
            {
                Console.WriteLine("Operation Cancelled");

                return;
            }

            string BackupDefinitionContent = File.ReadAllText(Files[0]);
            string DefinitionTemplatePath = $"C:/home/site/wwwroot/{WorkflowName}/workflow.json";

            File.WriteAllText(DefinitionTemplatePath, BackupDefinitionContent);

            Console.WriteLine("Revert finished, please refresh the workflow page");
        }
    }
}
