using Azure.Data.Tables;
using LogicAppAdvancedTool.Shared;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using Newtonsoft.Json;
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
            string selectedWorkflowId = WorkflowSelector.SelectFlowIDByName(workflowName);

            string selectedVersionID = WorkflowSelector.SelectVersionByFlowID(workflowName, selectedWorkflowId);

            Console.WriteLine($"Restoring workflow {workflowName} with ID {selectedWorkflowId} and version ID {selectedVersionID}.");

            TableEntity entitiesOfSelectedItem = TableOperations.QueryMainTable($"FlowName eq '{workflowName}' and FlowId eq '{selectedWorkflowId}' and FlowSequenceId eq '{selectedVersionID}'", select: new string[] { "FlowName", "ChangedTime", "DefinitionCompressed", "Kind", "RuntimeContext" }).FirstOrDefault();

            string flowName = entitiesOfSelectedItem.GetString("FlowName");
            string workflowPath = $"{AppSettings.RootFolder}\\{flowName}";

            CommonOperations.SaveDefinition(workflowPath, "workflow.json", entitiesOfSelectedItem);
            Console.WriteLine($"Workflow: {flowName} restored successfully, please refresh your workflow page.");


            byte[] runtimeContextBinary = entitiesOfSelectedItem.GetBinary("RuntimeContext");
            string runtimeContext = CommonOperations.DecompressContent(runtimeContextBinary);
            string formattedContent = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(runtimeContext), Formatting.Indented);

            string path = $"RuntimeContext_{workflowName}_{selectedVersionID}.json";
            File.WriteAllText(path, formattedContent);
            Console.WriteLine($"Runtime context for {workflowName} with version ID {selectedVersionID} saved to {path}. This file contains API connections related information which may need to be added in connections.json.");
        }
    }
}