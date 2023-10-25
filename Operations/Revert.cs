using Azure.Data.Tables;
using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.IO;

namespace LogicAppAdvancedTool.Operations
{
    public static class RevertVersion
    {
        public static void Run(string workflowName, string version)
        {
            List<TableEntity> tableEntities = TableOperations.QueryMainTable($"FlowSequenceId eq '{version}'");

            if (tableEntities.Count == 0)
            {
                throw new UserInputException($"No workflow definition found with version: {version}");
            }

            string confirmationMessage = $"WARNING!!!\r\nThe current workflow: {workflowName} will be overwrite!\r\nPlease input for confirmation:";
            if (!Prompt.GetYesNo(confirmationMessage, false, ConsoleColor.Red))
            {
                throw new UserCanceledException("Operation Cancelled");
            }

            TableEntity entity = tableEntities[0];
            byte[] definitionCompressed = entity.GetBinary("DefinitionCompressed");
            string kind = entity.GetString("Kind");
            string decompressedDefinition = CommonOperations.DecompressContent(definitionCompressed);
            string definition = $"{{\"definition\": {decompressedDefinition},\"kind\": \"{kind}\"}}";

            string definitionTemplatePath = $"C:/home/site/wwwroot/{workflowName}/workflow.json";

            File.WriteAllText(definitionTemplatePath, definition);

            Console.WriteLine("Revert finished, please refresh the workflow page");
        }
    }
}
