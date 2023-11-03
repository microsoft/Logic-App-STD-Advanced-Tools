using Azure.Data.Tables;
using System;
using System.IO;
using System.Linq;

namespace LogicAppAdvancedTool.Operations
{
    public static class RestoreSingleWorkflow
    {
        public static void Run(string workflowName)
        {
            TableEntity entity = TableOperations.QueryMainTable($"FlowName eq '{workflowName}'", select: new string[] { "FlowName", "ChangedTime", "DefinitionCompressed", "Kind" })
                                        .GroupBy(t => t.GetString("FlowName"))
                                        .Select(g => g.OrderByDescending(
                                            x => x.GetDateTimeOffset("ChangedTime"))
                                            .FirstOrDefault())
                                        .ToList()
                                        .FirstOrDefault();

            if (entity == null)
            {
                throw new UserInputException($"{workflowName} cannot be found in storage table, please check whether workflow is correct.");
            }

            string flowName = entity.GetString("FlowName");
            string workflowPath = $"{AppSettings.RootFolder}\\{flowName}";

            CommonOperations.SaveDefinition(workflowPath, "workflow.json", entity);

            Console.WriteLine($"Workflow: {flowName} restored successfully.");
        }
    }
}
