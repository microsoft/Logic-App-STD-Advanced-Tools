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

            string prefix = StoragePrefixGenerator.GenerateWorkflowTablePrefixByName(workflowName);
            string runTableName = $"flow{prefix}runs";

            TableClient runTableClient = new TableClient(AppSettings.ConnectionString, runTableName);

            string query = $"Status eq 'Running' or Status eq 'Waiting'";

            int totalCount = 0;
            PageableTableQuery queryForCount = new PageableTableQuery(AppSettings.ConnectionString, $"flow{StoragePrefixGenerator.GenerateWorkflowTablePrefixByName(workflowName)}runs", query, new string[] { "Status", "PartitionKey", "RowKey" }, 1000);
            while (queryForCount.HasNextPage)
            { 
                totalCount += queryForCount.GetNextPage().Count;
            }

            if (totalCount == 0)
            {
                throw new UserInputException($"There's no running/waiting runs of workflow {workflowName}");
            }

            Console.WriteLine($"Total {totalCount} running/waiting run(s) found. The final cancelled count might be slightly different due to workflow runs finished during cancellation.");

            CommonOperations.PromptConfirmation("1. Cancel all the running instances will cause data lossing for any running/waiting instances.\r\n2. Run history and resubmit feature will be unavailable for all waiting runs.");

            int cancelledCount = 0;
            int failedCount = 0;

            PageableTableQuery pageableTableQuery = new PageableTableQuery(AppSettings.ConnectionString, $"flow{StoragePrefixGenerator.GenerateWorkflowTablePrefixByName(workflowName)}runs", query, new string[] { "Status", "PartitionKey", "RowKey" }, 1000);
            while (pageableTableQuery.HasNextPage)
            {
                List<TableEntity> entities = pageableTableQuery.GetNextPage();

                //throw expected exception if no running/waiting instances found on the first page
                if (entities.Count == 0)
                {
                    
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
                        cancelledCount++;

                        //no accurate count for the total count of running/waiting instances, so just print the count to show the progress
                        if (cancelledCount % 1000 == 0)
                        {
                            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: {cancelledCount} runs has been cancelled, still processing");
                        }
                    }
                    catch
                    {
                        failedCount++;
                    }
                }
            }

            Console.WriteLine($"Cancellation has finished, total {cancelledCount} runs cancelled sucessfully");

            if (failedCount != 0)
            {
                Console.WriteLine($"{failedCount} runs cancelled failed due to status changed (it is an expected behavior while runs finished during canceling), please run command again to verify whether still have running instance or not.");
            }
        }
    }
}
