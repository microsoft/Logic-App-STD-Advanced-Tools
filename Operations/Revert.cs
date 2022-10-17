using System;
using System.IO;

namespace LAVersionReverter
{
    partial class Program
    {
        private static void RevertVersion(string WorkflowName, string Version)
        {
            string BackupFilePath = $"{Directory.GetCurrentDirectory()}/Backup/{WorkflowName}";
            string[] Files = Directory.GetFiles(BackupFilePath, $"*{Version}.json");

            if (Files == null || Files.Length == 0)
            {
                Console.WriteLine("No backup file found, please check the name and version of workflow");
            }

            string BackupDefinitionContent = File.ReadAllText(Files[0]);
            string DefinitionTemplatePath = $"C:/home/site/wwwroot/{WorkflowName}/workflow.json";

            File.WriteAllText(DefinitionTemplatePath, BackupDefinitionContent);

            Console.WriteLine("Revert finished, please refresh the workflow page");
        }
    }
}
