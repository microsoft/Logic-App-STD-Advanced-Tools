using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        private static void GenerateTablePrefix(string workflowName)
        {
            string logicAppPrefix = StoragePrefixGenerator.Generate(AppSettings.LogicAppName.ToLower());

            //if we don't need to generate workflow prefix, just output Logic App prefix
            if (String.IsNullOrEmpty(workflowName))
            {
                Console.WriteLine($"Logic App Prefix: {logicAppPrefix}");

                return;
            }

            List<TableEntity> tableEntities = TableOperations.QueryMainTable($"FlowName eq '{workflowName}'");

            if (tableEntities.Count() == 0)
            {
                throw new UserInputException($"{workflowName} cannot be found in storage table, please check whether workflow is correct.");
            }

            string workflowID = tableEntities.First<TableEntity>().GetString("FlowId");

            string workflowPrefix = StoragePrefixGenerator.Generate(workflowID);

            Console.WriteLine($"Logic App Prefix: {logicAppPrefix}");
            Console.WriteLine($"Workflow Prefix: {workflowPrefix}");
            Console.WriteLine($"Combined prefix: {logicAppPrefix}{workflowPrefix}");
        }
    }
}
