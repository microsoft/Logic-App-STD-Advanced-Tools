using McMaster.Extensions.CommandLineUtils;
using System;
using System.IO;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        private static void RevertVersion(string workflowName, string version)
        {
            string backupFilePath = $"{Directory.GetCurrentDirectory()}/Backup/{workflowName}";

            if (!Directory.Exists(backupFilePath))
            {
                throw new UserInputException($"{workflowName} folder cannot be found in wwwroot folder, please check the workflow name.");
            }

            string[] files = Directory.GetFiles(backupFilePath, $"*{version}.json");

            if (files == null || files.Length == 0)
            {
                throw new UserInputException("No backup file found, please check whether Version is correct.");
            }

            string confirmationMessage = $"WARNING!!!\r\nThe current workflow: {workflowName} will be overwrite\r\n\r\nPlease input for confirmation:";
            if (!Prompt.GetYesNo(confirmationMessage, false, ConsoleColor.Red))
            {
                Console.WriteLine("Operation Cancelled");

                return;
            }

            string backupDefinitionContent = File.ReadAllText(files[0]);
            string definitionTemplatePath = $"C:/home/site/wwwroot/{workflowName}/workflow.json";

            File.WriteAllText(definitionTemplatePath, backupDefinitionContent);

            Console.WriteLine("Revert finished, please refresh the workflow page");
        }
    }
}
