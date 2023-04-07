using McMaster.Extensions.CommandLineUtils;
using System;
using System.IO;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        private static void RevertVersion(string WorkflowName, string Version)
        {
            string BackupFilePath = $"{Directory.GetCurrentDirectory()}/Backup/{WorkflowName}";

            if (!Directory.Exists(BackupFilePath))
            {
                throw new UserInputException($"{WorkflowName} folder cannot be found in wwwroot folder, please check the workflow name.");
            }

            string[] Files = Directory.GetFiles(BackupFilePath, $"*{Version}.json");

            if (Files == null || Files.Length == 0)
            {
                throw new UserInputException("No backup file found, run Backup command first.");
            }

            string ConfirmationMessage = $"WARNING!!!\r\nThe current workflow: {WorkflowName} will be overwrite\r\n\r\nPlease input for confirmation:";
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
