﻿using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using McMaster.Extensions.CommandLineUtils;

namespace LogicAppAdvancedTool.Operations
{
    public static class CancelRuns
    {
        public static void Run(string workflowName)
        {
            CommonOperations.AlertExperimentalFeature();

            string query = $"Status eq 'Running' or Status eq 'Waiting'";
            List<TableEntity> inprocessRuns = TableOperations.QueryRunTable(workflowName, query, new string[] { "Status", "PartitionKey", "RowKey" });

            if (inprocessRuns.Count == 0)
            {
                throw new UserInputException($"There's no running/waiting runs of workflow {workflowName}");
            }

            Console.WriteLine($"Found {inprocessRuns.Count} run(s) in run table.");

            CommonOperations.PromptConfirmation("1. Cancel all the running instances will cause data lossing for any running/waiting instances.\r\n2. Run history and resubmit feature will be unavailable for all waiting runs.");

            string prefix = CommonOperations.GenerateWorkflowTablePrefix(workflowName);
            string runTableName = $"flow{prefix}runs";

            TableClient runTableClient = new TableClient(AppSettings.ConnectionString, runTableName);
            
            int CancelledCount = 0;
            int FailedCount = 0;

            foreach (TableEntity te in inprocessRuns)
            {
                TableEntity updatedEntity = new TableEntity
                {
                    { "Status", "Cancelled" }
                };

                updatedEntity.PartitionKey = te.PartitionKey;
                updatedEntity.RowKey = te.RowKey;

                //When instances status changed (eg: waiting -> running, running -> succeeded), the update will fail
                //it is an expected behavior, but we need to run the command again for verification
                try
                {
                    runTableClient.UpdateEntity<TableEntity>(updatedEntity, te.ETag);
                    CancelledCount++;
                }
                catch (Exception ex)
                {
                    FailedCount++;
                }
            }

            Console.WriteLine($"{CancelledCount} runs cancelled sucessfully");

            if (FailedCount != 0)
            { 
                Console.WriteLine($"{FailedCount} runs cancelled failed due to status changed (it is an expected behavior while runs finished during canceling), please run command again to verify whether still have running instance or not.");
            }
        }
    }
}
