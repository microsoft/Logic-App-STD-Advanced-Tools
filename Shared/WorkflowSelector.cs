using Azure.Data.Tables;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicAppAdvancedTool.Shared
{
    public class WorkflowSelector
    {
        public static string SelectFlowIDByName(string workflowName)
        {
            Console.WriteLine($"Retrieving all workflow ids which named {workflowName}.");

            List<TableEntity> entitiesOfWorkflow = WorkflowsInfoQuery.ListWorkflowsByName(workflowName);

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

                ConsoleTable workflowTable = new ConsoleTable(new List<string>() { "Flow ID", "Last Updated Time", "Kind", "Status" }, true);

                foreach (TableEntity entity in entitiesOfWorkflow)
                {
                    workflowTable.AddRow(new List<string>() { entity.GetString("FlowId"), entity.GetDateTimeOffset("ChangedTime")?.ToString("yyyy-MM-ddTHH:mm:ssZ"), entity.GetString("Kind"), currentFlowID == entity.GetString("FlowId") ? "In Use" : "Deleted" });
                }

                workflowTable.Print();

                Console.WriteLine("Please select the workflow you want to restore by entering the index.");

                //just decide not verify user input, assume user is smart enough to input correct index
                int selectedIndex = int.Parse(Console.ReadLine());
                selectedWorkflowId = entitiesOfWorkflow[selectedIndex - 1].GetString("FlowId");
            }

            return selectedWorkflowId;
        }

        public static string SelectVersionByFlowID(string workflowName, string workflowID)
        {
            Console.WriteLine($"Retrieving all versions of workflow {workflowName} with ID {workflowID}.");

            List<TableEntity> entitiesOfVersion = WorkflowsInfoQuery.ListVersionsByID(workflowID);

            if (entitiesOfVersion.Count == 0)
            {
                throw new UserInputException($"{workflowName} with id: {workflowID} cannot be found in storage table.");    //This should never happen
            }

            string selectedVersionID = string.Empty;

            if (entitiesOfVersion.Count == 1)
            {
                selectedVersionID = entitiesOfVersion[0].GetString("FlowSequenceId");
                Console.WriteLine($"Only one version found in {workflowID}, auto select id {selectedVersionID}.");
            }
            else
            {
                ConsoleTable consoleTable = new ConsoleTable(new List<string>() { "Version ID", "Updated Time" }, true);

                foreach (TableEntity entity in entitiesOfVersion)
                {
                    consoleTable.AddRow(new List<string> { entity.GetString("FlowSequenceId"), entity.GetDateTimeOffset("ChangedTime")?.ToString("yyyy-MM-ddTHH:mm:ssZ") });
                }

                consoleTable.Print();

                Console.WriteLine("Please select the workflow you want to restore by entering the index.");

                int selectedIndex = int.Parse(Console.ReadLine());
                selectedVersionID = entitiesOfVersion[selectedIndex - 1].GetString("FlowSequenceId");
            }

            return selectedVersionID;
        }
    }
}
