﻿using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;

namespace LogicAppAdvancedTool.Operations
{
    public static class RestoreRunHistory
    {
        public static void Run(string workflowName)
        {
            CommonOperations.AlertExperimentalFeature();

            List<TableEntity> entities = TableOperations.QueryMainTable($"FlowName eq '{workflowName}'", select: new string[] { "FlowName", "FlowId", "ChangedTime", "Kind" })
                                .GroupBy(t => t.GetString("FlowId"))
                                .Select(g => g.OrderByDescending(
                                    x => x.GetDateTimeOffset("ChangedTime"))
                                    .FirstOrDefault())
                                .ToList();

            if (entities.Count == 0)
            {
                throw new UserInputException("No workflow found. Please review your input or use \"ListWorkflows\" command to retrieve all existing worklfows in Storage Table.");
            }

            ConsoleTable consoleTable = new ConsoleTable("Index", "Workflow Name", "Flow ID", "Last Updated (UTC)");

            int index = 0;

            foreach (TableEntity entity in entities)
            {
                string flowName = entity.GetString("FlowName");
                string changedTime = entity.GetDateTimeOffset("ChangedTime")?.ToString("yyyy-MM-ddTHH:mm:ssZ");
                string flowID = entity.GetString("FlowId");
                string kind = entity.GetString("Kind");

                consoleTable.AddRow((++index).ToString(), flowName, flowID, changedTime);
            }

            consoleTable.Print();

            string selectedWorkflowID;
            if (entities.Count == 1)
            {
                selectedWorkflowID = entities[0].GetString("FlowId");
                Console.WriteLine($"Only 1 workflow found, using default workflow id: {selectedWorkflowID}");
            }
            else
            {
                Console.WriteLine($"There are {entities.Count} worklfows found in Storage Table, due to workflow overwritten (delete and create workflow with same name).");
                Console.WriteLine("Please enter the Index which you would like to restore the run history");

                int rowID = Int32.Parse(Console.ReadLine());
                selectedWorkflowID = entities[rowID - 1].GetString("FlowId");
            }

            Console.WriteLine($"Generating new workflow for restoring run history with workflow id {selectedWorkflowID}");

            string reviewerWorkflowName = $"AutoGenerated_HistoryViewer_{DateTime.Now.ToString("yyyyMMddHHmmss")}";
            string reviewerWorkflowPath = $"{AppSettings.RootFolder}\\{reviewerWorkflowName}";
            string reviewerWorkflowDefinition = CommonOperations.GetEmbeddedResource("LogicAppAdvancedTool.Resources.EmptyDefinition.json");

            if (!Directory.Exists(reviewerWorkflowPath))
            { 
                Directory.CreateDirectory(reviewerWorkflowPath);
            }

            File.WriteAllText($"{reviewerWorkflowPath}/workflow.json", reviewerWorkflowDefinition);

            Console.WriteLine($"Workflow {reviewerWorkflowName} has been created in File Share, retrieving data from Storage Table...");

            List<TableEntity> reviewerWorkflowEntities = new List<TableEntity>();

            //After create an empty workflow, it might take several seconds to update Storage Table
            //try 10 times to retrieve newly create worklfow id
            for (int i = 1; i <= 10; i++)
            {
                reviewerWorkflowEntities = TableOperations.QueryMainTable($"FlowName eq '{reviewerWorkflowName}'");
                if (reviewerWorkflowEntities.Count != 0)
                {
                    break;
                }

                if (i == 10)
                {
                    Console.WriteLine("Failed to retrieve records from Storage Table, please re-execute the command.");

                    return;
                }

                Console.WriteLine($"Records not ingested into Storage Table yet, retry after 5 seconds, execution count {i}/10");
                Thread.Sleep(5000);
            }

            Console.WriteLine("Records found, converting...");

            TableClient tableClient = new TableClient(AppSettings.ConnectionString, TableOperations.DefinitionTableName);

            foreach (TableEntity te in reviewerWorkflowEntities)
            {
                TableEntity updatedEntity = new TableEntity
                {
                    { "FlowId", selectedWorkflowID }
                };

                updatedEntity.PartitionKey = te.PartitionKey;
                updatedEntity.RowKey = te.RowKey;

                tableClient.UpdateEntity<TableEntity>(updatedEntity, te.ETag);
            }

            Console.WriteLine($"Restore succeeded, please refresh workflow page and open workflow {workflowName} for reviewing.");
        }
    }
}
