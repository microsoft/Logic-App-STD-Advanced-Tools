using Azure.Data.Tables;
using LogicAppAdvancedTool.Shared;
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
            List<TableEntity> entities = WorkflowsInfoQuery.ListAllWorkflows("FlowName");

            ConsoleTable consoleTable = new ConsoleTable(new List<string>() { "Workflow Name", "Last Updated (UTC)", "Workflow Count" }, true);

            foreach (TableEntity entity in entities)
            {
                string flowName = entity.GetString("FlowName");
                string changedTime = entity.GetDateTimeOffset("ChangedTime")?.ToString("yyyy-MM-ddTHH:mm:ssZ");

                List<TableEntity> flowsWithSameName = WorkflowsInfoQuery.ListWorkflowsByName(flowName);

                consoleTable.AddRow(new List<string>() { flowName, changedTime, flowsWithSameName.Count.ToString() });
            }

            consoleTable.Print();

            Console.WriteLine("The workflow count is the total workflows detected with same workflow name but different FlowId which includes deleted workflows.");
            int index = CommonOperations.PromptInput(entities.Count, "Enter index to list all workflows with same name.");

            string selectedWorkflowName = entities[index].GetString("FlowName");

            List<TableEntity> entitiesOfWorkflow = WorkflowsInfoQuery.ListWorkflowsByName(selectedWorkflowName);

            Console.WriteLine($"All workflows named {selectedWorkflowName} based on workflow ID:");
            
            ConsoleTable workflowTable = new ConsoleTable(new List<string>() { "Flow ID", "Last Updated (UTC)", "Kind", "Status" }, true);
            string currentFlowID = TableOperations.QueryCurrentWorkflowByName(selectedWorkflowName).FirstOrDefault()?.GetString("FlowId");

            foreach (TableEntity entity in entitiesOfWorkflow)
            {
                string flowId = entity.GetString("FlowId");

                workflowTable.AddRow(new List<string>() { flowId, entity.GetDateTimeOffset("ChangedTime")?.ToString("yyyy-MM-ddTHH:mm:ssZ"), entity.GetString("Kind"), currentFlowID == flowId ? "In Use" : "Deleted" });
            }

            workflowTable.Print();

            index = CommonOperations.PromptInput(entitiesOfWorkflow.Count, "Enter index to list all versions of selected workflow id.");
            string selectedWorkflowId = entitiesOfWorkflow[index].GetString("FlowId");

            List<TableEntity> entitiesOfVersions = WorkflowsInfoQuery.ListVersionsByID(selectedWorkflowId);
            ConsoleTable versionTable = new ConsoleTable(new List<string>() { "Version ID", "Last Updated (UTC)" });

            foreach (TableEntity entity in entitiesOfVersions)
            {
                versionTable.AddRow(new List<string>() { entity.GetString("FlowSequenceId"), entity.GetDateTimeOffset("ChangedTime")?.ToString("yyyy-MM-ddTHH:mm:ssZ") });
            }

            versionTable.Print();
        }
    }
}