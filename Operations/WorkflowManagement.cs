using Azure.Data.Tables;
using LogicAppAdvancedTool.Structures;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace LogicAppAdvancedTool.Operations
{
    public static class WorkflowManagement
    {
        public static void Run(string operation)
        {
            switch (operation)
            {
                case "disable":
                    DisableWorkflows();
                    break;
                case "restore":
                    RestoreWorkflows();
                    break;
            }
        }

        #region Disable workflows
        private static void DisableWorkflows()
        {
            string listWorkflowsUrl = AppSettings.ManagementBaseUrl + $"/hostruntime/runtime/webhooks/workflow/api/management/workflows?api-version=2018-11-01";
            MSIToken token = MSITokenService.RetrieveToken("https://management.azure.com");
            Console.WriteLine("Managed Identity token retrieved");

            string listWorkflowsResponse = HttpOperations.ValidatedHttpRequestWithToken(listWorkflowsUrl, HttpMethod.Get, string.Empty, token.access_token, "Cannot get workflow list, please check managed indentity permission.");
            Console.WriteLine("Workflow list has been retrieved");

            List<WorkflowInfo> workflowInfos = JsonConvert.DeserializeObject<List<WorkflowInfo>>(listWorkflowsResponse);

            if (workflowInfos == null || workflowInfos.Count == 0)
            {
                throw new ExpectedException("No workflow found in this Logic App.");
            }

            JObject appSettings = JObject.Parse(AppSettings.GetRemoteAppsettings());

            List<JObject> workflowStatus = new List<JObject>();

            foreach (WorkflowInfo info in workflowInfos)
            {
                string envVariableName = $"Workflows.{info.name}.FlowState";

                if (appSettings[envVariableName] != null)
                {
                    appSettings[envVariableName] = "Disabled";

                    workflowStatus.Add(new JObject()
                    {
                        { "WorkflowName", info.name },
                        { "Status",  appSettings[envVariableName] }
                    });
                }
                else
                {
                    appSettings.Add(envVariableName, "Disabled");

                    workflowStatus.Add(new JObject()
                    {
                        { "WorkflowName", info.name },
                        { "Status",  "Enabled" }
                    });
                }
            }

            string appsettingContent = JsonConvert.SerializeObject(appSettings, Formatting.Indented);

            AppSettings.UpdateRemoteAppsettings(appsettingContent);
            Console.WriteLine("Appsettings updated, all workflows have been disabled.");

            string fileName = $"WorkflowsStatus.json";

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
                Console.WriteLine($"File already exists, the previous file has been deleted");
            }

            File.AppendAllText(fileName, JsonConvert.SerializeObject(workflowStatus, Formatting.Indented));
            Console.WriteLine($"Previous workflows status has been saved in file named {fileName}");
        }
        #endregion

        #region Restore workflows
        public static void RestoreWorkflows()
        {
            string fileName = "WorkflowsStatus.json";
            if (!File.Exists(fileName))
            {
                throw new ExpectedException($"Workflow satus file: {fileName} doesn't exist, canceling operation.");
            }

            string workflowStatus = File.ReadAllText(fileName);
            List<JObject> workflowStatusToken = JsonConvert.DeserializeObject<List<JObject>>(workflowStatus);

            string appsettingContent = AppSettings.GetRemoteAppsettings();
            JObject appSettings = JObject.Parse(appsettingContent);

            foreach (JObject token in workflowStatusToken)
            { 
                string envVariableName = $"Workflows.{token["WorkflowName"]}.FlowState";
                string status = token["Status"].ToString();

                if (appSettings[envVariableName] != null)
                {
                    appSettings[envVariableName] = status;
                }
                else
                {
                    appSettings.Add(envVariableName, status);
                }
            }

            AppSettings.UpdateRemoteAppsettings(JsonConvert.SerializeObject(appSettings, Formatting.Indented));
            Console.WriteLine("Appsettings updated, all workflows status have been restored as previous situation.");
        }
        #endregion
    }
}
