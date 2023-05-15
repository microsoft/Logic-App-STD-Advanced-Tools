using Azure.Data.Tables;
using Azure;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        public static void IngestWorkflow(string LogicAppName, string WorkflowName)
        {
            string WorkflowPath = $"C:/home/site/wwwroot/{WorkflowName}/workflow.json";

            if (!File.Exists(WorkflowPath))
            {
                Console.WriteLine($"Cannot find definition Json file based on workflow path ({WorkflowPath}), please check the Workflow name and verify whether file exists in Kudu");

                return;
            }

            string Content = File.ReadAllText(WorkflowPath);

            WorkflowTemplate Template = JsonConvert.DeserializeObject<WorkflowTemplate>(Content);
            string Definition = JsonConvert.SerializeObject(Template.definition);
            byte[] CompressedDefinition = CompressContent(Definition);

            string MainTableName = GetMainTableName(LogicAppName);

            string BackupPath = BackupCurrentSite();
            Console.WriteLine($"Backup current workflows, you can find in path: {BackupPath}");

            TableClient tableClient = new TableClient(connectionString, MainTableName);
            Pageable<TableEntity> mainTableEntities = tableClient.Query<TableEntity>(filter: $"FlowName eq '{WorkflowName}'");
            List<TableEntity> mainLatestEntities = mainTableEntities.OrderByDescending(tableEntity => tableEntity.GetDateTimeOffset("ChangedTime")).Take(4).ToList();

            DateTimeOffset currentTime = DateTimeOffset.Now;

            foreach (TableEntity entity in mainLatestEntities)
            { 
                TableEntity newEntity = new TableEntity(entity.PartitionKey, entity.RowKey);
                newEntity.Add("DefinitionCompressed", CompressedDefinition);
                newEntity.Add("ChangedTime", currentTime);
                tableClient.UpdateEntity(newEntity, entity.ETag, TableUpdateMode.Merge);
            }

            string logicAppPrefix = GetMainTablePrefix(LogicAppName);
            string workflowID = mainTableEntities.First<TableEntity>().GetString("FlowId");
            string workflowPrefix = StoragePrefixGenerator.Generate(workflowID);
            string workflowTableName = $"flow{logicAppPrefix}{workflowPrefix}flows";

            tableClient = new TableClient(connectionString, workflowTableName);
            Pageable<TableEntity> wfTableEntities = tableClient.Query<TableEntity>(filter: $"FlowName eq '{WorkflowName}'");
            List<TableEntity> wfLatestEntities = wfTableEntities.OrderByDescending(tableEntity => tableEntity.GetDateTimeOffset("ChangedTime")).Take(2).ToList();

            foreach (TableEntity entity in wfLatestEntities)
            {
                TableEntity newEntity = new TableEntity(entity.PartitionKey, entity.RowKey);
                newEntity.Add("DefinitionCompressed", CompressedDefinition);
                newEntity.Add("ChangedTime", currentTime);
                tableClient.UpdateEntity(newEntity, entity.ETag, TableUpdateMode.Merge);
            }
        }
    }
}
