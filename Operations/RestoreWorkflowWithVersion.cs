using Azure.Data.Tables;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LogicAppAdvancedTool.Operations
{
    public static class RestoreWorkflowWithVersion
    {
        public static void Run(string workflowName)
        {
            Console.WriteLine($"Retrieving all workflows named {workflowName} based on workflow ID.");

            List<TableEntity> entitiesOfWorkflow = TableOperations.QueryMainTable($"FlowName eq '{workflowName}'", select: new string[] { "RowKey", "FlowUpdatedTime", "FlowId", "Kind" })
                                        .GroupBy(t => t.GetString("FlowId"))
                                        .Select(g => g.OrderByAscending(
                                            x => x.GetDateTimeOffset("FlowUpdatedTime"))
                                            .FirstOrDefault())
                                        .ToList();

            if (entitiesOfWorkflow.Count == 0)
            {
                throw new UserInputException($"{workflowName} cannot be found in storage table, please check whether workflow is correct.");
            }

            string selectedWorkflowId = string.Empty;
            if (entitiesOfWorkflow.Count == 1)
            {
                selectedWorkflowId = entitiesOfWorkflow[0].GetString("FlowId");
                Console.WriteLine($"Only one workflow named {workflowName} found, auto select id {selectedWorkflowId}.");
            }
            else
            {
                TableEntity currentWorkflow = TableOperations.QueryMainTable($"RowKey eq 'MYEDGEENVIRONMENT_FLOWLOOKUP-MYEDGERESOURCEGROUP-{workflowName.ToUpper()}'", select: new string[] { "FlowId" }).FirstOrDefault();

                string currentFlowID = string.Empty;
                if (currentWorkflow != null) 
                { 
                    currentFlowID = currentWorkflow.GetString("FlowId");
                }

                ConsoleTable workflowTable = new ConsoleTable("Index", "Flow ID", "Created Time", "Kind", "Status");

                int index = 1;
                foreach (TableEntity entity in entitiesOfWorkflow)
                {
                    workflowTable.AddRow((index++).ToString(), entity.GetString("FlowId"), entity.GetDateTimeOffset("FlowUpdatedTime")?.ToString("yyyy-MM-ddTHH:mm:ssZ"), entity.GetString("Kind"), currentFlowID == entity.GetString("FlowId") ? "In Use" : "Deleted");
                }

                workflowTable.Print();

                Console.WriteLine("Please select the workflow you want to restore by entering the index.");

                int selectedIndex = int.Parse(Console.ReadLine());
                selectedWorkflowId = entitiesOfWorkflow[selectedIndex - 1].GetString("FlowId");
            }

            Console.WriteLine($"Retrieving all versions of workflow {workflowName} with ID {selectedWorkflowId}.");

            List<TableEntity> entitiesOfVersion = TableOperations.QueryMainTable($"FlowId eq '{selectedWorkflowId}'", select: new string[] { "RowKey", "FlowUpdatedTime", "FlowSequenceId", "Kind" })
                            .Where(t => t.GetString("RowKey").Contains("FLOWVERSION"))
                            .GroupBy(t => t.GetString("FlowSequenceId"))
                            .Select(g => g.OrderByAscending(
                                x => x.GetDateTimeOffset("FlowUpdatedTime"))
                                .FirstOrDefault())
                            .ToList();

            if (entitiesOfVersion.Count == 0) 
            {
                throw new UserInputException($"{workflowName} with id: {selectedWorkflowId} cannot be found in storage table.");    //This should never happen
            }

            string selectedVersionID = string.Empty;

            if (entitiesOfVersion.Count == 1)
            {
                selectedVersionID = entitiesOfWorkflow[0].GetString("FlowSequenceId");
                Console.WriteLine($"Only one version found in {selectedWorkflowId}, auto select id {selectedVersionID}.");
            }
            else
            { 
                ConsoleTable consoleTable = new ConsoleTable("Index", "Version ID", "Updated Time");
                int index = 1;

                foreach (TableEntity entity in entitiesOfVersion)
                {
                    consoleTable.AddRow(index++.ToString(), entity.GetString("FlowSequenceId"), entity.GetDateTimeOffset("FlowUpdatedTime")?.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                }

                consoleTable.Print();

                Console.WriteLine("Please select the workflow you want to restore by entering the index.");

                int selectedIndex = int.Parse(Console.ReadLine());
                selectedVersionID = entitiesOfVersion[selectedIndex - 1].GetString("FlowSequenceId");
            }

            Console.WriteLine($"Restoring workflow {workflowName} with ID {selectedWorkflowId} and version ID {selectedVersionID}.");

            TableEntity entitiesOfSelectedItem = TableOperations.QueryMainTable($"FlowName eq '{workflowName}' and FlowId eq '{selectedWorkflowId}' and FlowSequenceId eq '{selectedVersionID}'", select: new string[] { "FlowName", "ChangedTime", "DefinitionCompressed", "Kind" }).FirstOrDefault();

            string flowName = entitiesOfSelectedItem.GetString("FlowName");
            string workflowPath = $"{AppSettings.RootFolder}\\{flowName}";

            CommonOperations.SaveDefinition(workflowPath, "workflow.json", entitiesOfSelectedItem);
            Console.WriteLine($"Workflow: {flowName} restored successfully, please refresh your workflow page.");
        }
    }
}
