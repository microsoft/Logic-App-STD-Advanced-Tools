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
        private static void Clone(string sourceName, string targetName, string version)
        {
            string identity = string.IsNullOrEmpty(version) ? "FLOWIDENTIFIER" : version.ToUpper();

            TableEntity entity = TableOperations.QueryMainTable($"FlowName eq '{sourceName}'")
                                                .Where(t => t.GetString("RowKey").Contains(identity))
                                                .FirstOrDefault();

            if (entity == null)
            {
                throw new UserInputException("No workflow found, please check workflow name and version is correct or not");
            }

            string rowKey = entity.GetString("RowKey");

            byte[] definitionCompressed = entity.GetBinary("DefinitionCompressed");
            string kind = entity.GetString("Kind");
            string decompressedDefinition = DecompressContent(definitionCompressed);

            string outputContent = $"{{\"definition\": {decompressedDefinition},\"kind\": \"{kind}\"}}";
            string clonePath = $"C:/home/site/wwwroot/{targetName}";

            if (Directory.Exists(clonePath))
            {
                throw new UserInputException("Workflow already exists, workflow will not be cloned. Please use another target name.");
            }

            Directory.CreateDirectory(clonePath);
            File.WriteAllText($"{clonePath}/workflow.json", outputContent);


            Console.WriteLine("Clone finished, please refresh workflow page");
        }
    }
}
