using Azure.Data.Tables;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LogicAppAdvancedTool.Operations
{
    public static class ListWorkflows
    {
        public static void Run()
        {
            List<TableEntity> entities = TableOperations.QueryMainTable(null, select: new string[] { "FlowName", "ChangedTime", "Kind" })
                                .GroupBy(t => t.GetString("FlowName"))
                                .Select(g => g.OrderByDescending(
                                    x => x.GetDateTimeOffset("ChangedTime"))
                                    .FirstOrDefault())
                                .ToList();

            if (entities.Count == 0)
            {
                throw new UserInputException("No workflow found.");
            }

            ConsoleTable consoleTable = new ConsoleTable(new List<string>() { "Workflow Name", "Last Updated (UTC)", "Workflow Count" }, true);

            foreach (TableEntity entity in entities)
            {
                string flowName = entity.GetString("FlowName");
                string changedTime = entity.GetDateTimeOffset("ChangedTime")?.ToString("yyyy-MM-ddTHH:mm:ssZ");

                List<TableEntity> flowsWithSameName = TableOperations.QueryMainTable($"FlowName eq '{flowName}'", select: new string[] { "FlowId" })
                                                        .DistinctBy( t => t.GetString("FlowId"))
                                                        .ToList();

                consoleTable.AddRow(new List<string>() { flowName, changedTime, flowsWithSameName.Count.ToString() });
            }

            consoleTable.Print();

            Console.WriteLine("Please note that the workflow count is the total workflows detected with same workflow name based on FlowId which also include deleted workflows.");

            Console.WriteLine("If you would like to list all workflows with same name, please enter Index. Press Ctrl + C to stop.");
            string cmd = Console.ReadLine();

            int index = 0;
            if (!int.TryParse(cmd, out index))
            {
                Console.WriteLine("Operation canceled");

                return;
            }

            string selectedWorkflowName = entities[index - 1].GetString("FlowName");

            List<TableEntity> entitiesOfWorkflow = TableOperations.QueryMainTable($"FlowName eq '{selectedWorkflowName}'", select: new string[] { "RowKey", "ChangedTime", "FlowId", "Kind" })
                                        .GroupBy(t => t.GetString("FlowId"))
                                        .Select(g => g.OrderByDescending(x => x.GetDateTimeOffset("ChangedTime"))
                                                .FirstOrDefault()
                                                )
                                        .ToList();

            Console.WriteLine($"All workflows named {selectedWorkflowName} based on workflow ID:");
            ConsoleTable workflowTable = new ConsoleTable(new List<string>() { "Flow ID", "Last Updated (UTC)", "Kind", "Status" }, true);

            string currentFlowID = TableOperations.QueryCurrentWorkflowByName(selectedWorkflowName).FirstOrDefault()?.GetString("FlowId");

            foreach (TableEntity entity in entitiesOfWorkflow)
            {
                string flowId = entity.GetString("FlowId");

                workflowTable.AddRow(new List<string>() { flowId, entity.GetDateTimeOffset("ChangedTime")?.ToString("yyyy-MM-ddTHH:mm:ssZ"), entity.GetString("Kind"), currentFlowID==flowId?"In Use" : "Deleted" });
            }

            workflowTable.Print();

            Console.WriteLine("If you would like to list all versions of a specific workflow id, please enter Index. Press Ctrl + C to stop.");
            cmd = Console.ReadLine();

            if (!int.TryParse(cmd, out index))
            {
                Console.WriteLine("Operation canceled");

                return;
            }
            
            string selectedWorkflowId = entitiesOfWorkflow[index - 1].GetString("FlowId");

            List<TableEntity> entitiesOfVersions = TableOperations.QueryMainTable($"FlowId eq '{selectedWorkflowId}'", select: new string[] { "RowKey", "ChangedTime", "FlowSequenceId" })
                            .Where( t => t.GetString("RowKey").StartsWith("MYEDGEENVIRONMENT_FLOWVERSION"))
                            .OrderByDescending(t => t.GetDateTimeOffset("ChangedTime"))
                            .ToList();

            ConsoleTable versionTable = new ConsoleTable(new List<string>() { "Version ID", "Last Updated (UTC)" });

            foreach(TableEntity entity in entitiesOfVersions)
            {
                versionTable.AddRow(new List<string>() { entity.GetString("FlowSequenceId"), entity.GetDateTimeOffset("ChangedTime")?.ToString("yyyy-MM-ddTHH:mm:ssZ") });
            }

            versionTable.Print();
        }
    }
}