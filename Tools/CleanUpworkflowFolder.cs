using Azure.Data.Tables;
using LogicAppAdvancedTool.Shared;
using LogicAppAdvancedTool.Structures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace LogicAppAdvancedTool
{
    internal partial class Tools
    {
        public static void CleanUpWorkflowFolder()
        {
            List<string> allWorkflows = WorkflowsInfoQuery.ListCurrentWorkflows()
                                                           .Select(t => t.GetString("FlowName"))
                                                           .ToList();

            string workflowFolder = AppSettings.RootFolder;

            CommonOperations.PromptConfirmation($"Are you sure you want to delete all files (exception workflow.json) and sub-folders in workflow folders?");

            foreach (string workflow in allWorkflows)
            {
                string workflowPath = Path.Combine(workflowFolder, workflow);
                
                string[] subFolders = Directory.GetDirectories(workflowPath);
                foreach (string subFolder in subFolders)
                { 
                    Directory.Delete(subFolder, true);
                }

                string[] files = Directory.GetFiles(workflowPath);
                foreach (string file in files)
                {
                    if (file.EndsWith("workflow.json"))
                    {
                        continue;
                    }

                    File.Delete(file);
                }
            }

            Console.WriteLine("All workflow folders have been cleaned up.");
        }
    }
}