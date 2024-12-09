using Azure.Data.Tables;
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

            CommonOperations.PromptConfirmation("1. Cancel all the running instances will cause data lossing for any running/waiting instances.\r\n2. Run history and resubmit feature will be unavailable for all waiting runs.");

            string prefix = CommonOperations.GenerateWorkflowTablePrefix(workflowName);
            string runTableName = $"flow{prefix}runs";

            TableClient runTableClient = new TableClient(AppSettings.ConnectionString, runTableName);

            int CancelledCount = 0;
            int FailedCount = 0;
            string query = $"Status eq 'Running' or Status eq 'Waiting'";

            PageableTableQuery pageableTableQuery = new PageableTableQuery(AppSettings.ConnectionString, $"flow{CommonOperations.GenerateWorkflowTablePrefix(workflowName)}runs", query, new string[] { "Status", "PartitionKey", "RowKey" }, 1000);
            while (pageableTableQuery.HasNextPage)
            {
                List<TableEntity> entities = pageableTableQuery.GetNextPage();

                //The count only can be 0 when query the first page which means there's no running/waiting runs of workflow
                if (entities.Count == 0)
                {
                    throw new UserInputException($"There's no running/waiting runs of workflow {workflowName}");
                }

                foreach (TableEntity te in entities)
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

                        //no accurate count for the total count of running/waiting instances, so just print the count to show the progress
                        if (CancelledCount % 1000 == 0)
                        {
                            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: {CancelledCount} runs has been cancelled, still processing");
                        }
                    }
                    catch (Exception ex)
                    {
                        FailedCount++;
                    }
                }
            }

            Console.WriteLine($"Cancellation has finished, total {CancelledCount} runs cancelled sucessfully");

            if (FailedCount != 0)
            {
                Console.WriteLine($"{FailedCount} runs cancelled failed due to status changed (it is an expected behavior while runs finished during canceling), please run command again to verify whether still have running instance or not.");
            }
        }
    }
}
