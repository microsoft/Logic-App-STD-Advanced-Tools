using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using System;
using System.IO;
using System.Linq;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        private static void GenerateTablePrefix(string LogicAppName, string WorkflowName)
        {
            string logicAppPrefix = StoragePrefixGenerator.Generate(LogicAppName.ToLower());

            //if we don't need to generate workflow prefix, just output Logic App prefix
            if (String.IsNullOrEmpty(WorkflowName))
            {
                Console.WriteLine($"Logic App Prefix: {logicAppPrefix}");

                return;
            }

            string mainTableName = GetMainTableName(LogicAppName);

            TableClient tableClient = new TableClient(ConnectionString, mainTableName);
            Pageable<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: $"FlowName eq '{WorkflowName}'");

            if (tableEntities.Count() == 0)
            {
                throw new UserInputException($"{WorkflowName} cannot be found in storage table, please check whether workflow is correct.");
            }

            string workflowID = tableEntities.First<TableEntity>().GetString("FlowId");

            string workflowPrefix = StoragePrefixGenerator.Generate(workflowID);

            Console.WriteLine($"Logic App Prefix: {logicAppPrefix}");
            Console.WriteLine($"Workflow Prefix: {workflowPrefix}");
            Console.WriteLine($"Combined prefix: {logicAppPrefix}{workflowPrefix}");
        }
    }
}
