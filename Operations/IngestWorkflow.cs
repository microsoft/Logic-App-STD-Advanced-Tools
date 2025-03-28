using Azure.Data.Tables;
using LogicAppAdvancedTool.Structures;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LogicAppAdvancedTool.Operations
{
    public static class IngestWorkflow
    {
        public static void Run(string workflowName)
        {
            CommonOperations.AlertExperimentalFeature();

            string WorkflowPath = $"{AppSettings.RootFolder}\\{workflowName}\\workflow.json";

            if (!File.Exists(WorkflowPath))
            {
                Console.WriteLine($"Cannot find definition Json file based on workflow path ({WorkflowPath}), please check the Workflow name and verify whether file exists in Kudu");

                return;
            }

            string content = File.ReadAllText(WorkflowPath);

            WorkflowTemplate template = JsonConvert.DeserializeObject<WorkflowTemplate>(content);
            string definition = JsonConvert.SerializeObject(template.definition);
            byte[] compressedDefinition = CommonOperations.CompressContent(definition);

            string backupPath = CommonOperations.BackupCurrentSite();
            Console.WriteLine($"Backup current workflows, you can find in path: {backupPath}");

            List<TableEntity> mainLatestEntities = TableOperations.QueryMainTable($"FlowName eq '{workflowName}'")
                                                    .OrderByDescending(tableEntity => tableEntity.GetDateTimeOffset("ChangedTime"))
                                                    .Take(4)
                                                    .ToList();

            DateTimeOffset currentTime = DateTimeOffset.Now;

            //TableClient tableClient = new TableClient(AppSettings.ConnectionString, TableOperations.DefinitionTableName);
            TableClient tableClient = TableOperations.GenerateTableClient(TableOperations.DefinitionTableName);

            foreach (TableEntity entity in mainLatestEntities)
            { 
                TableEntity newEntity = new TableEntity(entity.PartitionKey, entity.RowKey);
                newEntity.Add("DefinitionCompressed", compressedDefinition);
                newEntity.Add("ChangedTime", currentTime);
                tableClient.UpdateEntity(newEntity, entity.ETag, TableUpdateMode.Merge);
            }

            List<TableEntity> wfLatestEntities = TableOperations.QueryWorkflowTable(workflowName, $"FlowName eq '{workflowName}'")
                                                    .OrderByDescending(tableEntity => tableEntity.GetDateTimeOffset("ChangedTime"))
                                                    .Take(2)
                                                    .ToList();

            foreach (TableEntity entity in wfLatestEntities)
            {
                TableEntity newEntity = new TableEntity(entity.PartitionKey, entity.RowKey);
                newEntity.Add("DefinitionCompressed", compressedDefinition);
                newEntity.Add("ChangedTime", currentTime);
                tableClient.UpdateEntity(newEntity, entity.ETag, TableUpdateMode.Merge);
            }
        }
    }
}
