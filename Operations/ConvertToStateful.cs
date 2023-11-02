using Azure.Data.Tables;
using System;
using System.IO;
using System.Linq;

namespace LogicAppAdvancedTool.Operations
{
    public static class ConvertToStateful
    {
        public static void Run(string sourceName, string targetName)
        {
            TableEntity entity = TableOperations.QueryMainTable($"FlowName eq '{sourceName}'")
                                                        .Where(t => t.GetString("RowKey").Contains("FLOWIDENTIFIER"))
                                                        .FirstOrDefault();


            byte[] definitionCompressed = entity.GetBinary("DefinitionCompressed");
            string decompressedDefinition = CommonOperations.DecompressContent(definitionCompressed);

            string outputContent = $"{{\"definition\": {decompressedDefinition},\"kind\": \"Stateful\"}}";
            string clonePath = $"{AppSettings.RootFolder}\\{targetName}";

            if (Directory.Exists(clonePath))
            {
                throw new UserInputException("Workflow already exists, workflow will not be cloned. Please use another target name.");
            }

            Directory.CreateDirectory(clonePath);
            File.WriteAllText($"{clonePath}/workflow.json", outputContent);

            Console.WriteLine("Convert finished, please refresh workflow page");
        }
    }
}
