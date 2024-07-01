using Azure.Data.Tables;
using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LogicAppAdvancedTool.Operations
{
    public static class RevertVersion
    {
        public static void Run(string workflowName, string version)
        {
            TableEntity entity = TableOperations.QueryMainTable($"FlowSequenceId eq '{version}'").FirstOrDefault();

            if (entity == null)
            {
                throw new UserInputException($"No workflow definition found with version: {version}");
            }

            CommonOperations.PromptConfirmation($"The current workflow: {workflowName} will be overwrite!");

            CommonOperations.SaveDefinition($"{AppSettings.RootFolder}\\{workflowName}", "workflow.json", entity);

            Console.WriteLine("Revert finished, please refresh the workflow page");
        }
    }
}
