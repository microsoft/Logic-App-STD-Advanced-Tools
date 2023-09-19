using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using System.Collections.Generic;
using System.Linq;
using static LogicAppAdvancedTool.Program;

namespace LogicAppAdvancedTool
{
    public class TableOperations
    {
        public static string DefinitionTableName 
        {
            get
            {
                return $"flow{StoragePrefixGenerator.Generate(AppSettings.LogicAppName.ToLower())}flows";
            }
        }

        public static void UpdateTable()
        { 
            //TODO
        }

        private static List<TableEntity> QueryTable(string tableName, string query, string[] select = null)
        {
            List<TableEntity> tableEntities = new List<TableEntity>();

            TableServiceClient serviceClient = new TableServiceClient(AppSettings.ConnectionString);
            Pageable<TableItem> results = serviceClient.Query(filter: $"TableName eq '{tableName}'");

            if (results.Count() == 0)
            {
                throw new UserInputException($"Table - {tableName} not exist, please check whether parameters are correct or not.");
            }

            TableClient tableClient = new TableClient(AppSettings.ConnectionString, tableName);

            tableEntities = tableClient.Query<TableEntity>(filter: query, select: select).ToList();

            return tableEntities;
        }

        public static List<TableEntity> QueryHistoryTable(string workflowName, string filter, string[] select = null)
        {
            string historyTableName = $"flow{GenerateWorkflowTablePrefix(workflowName)}histories";

            return QueryTable(historyTableName, filter, select);
        }

        public static List<TableEntity> QueryRunTable(string workflowName, string filter, string[] select = null)
        {
            string runTableName = $"flow{GenerateWorkflowTablePrefix(workflowName)}runs";

            return QueryTable(runTableName, filter, select);
        }

        public static List<TableEntity> QueryActionTable(string workflowName, string date, string filter, string[] select = null)
        { 
            string actionTableName = $"flow{GenerateWorkflowTablePrefix(workflowName)}{date}t000000zactions";

            return QueryTable(actionTableName, filter, select);
        }

        public static List<TableEntity> QueryWorkflowTable(string workflowName, string filter, string[] select = null)
        { 
            string workflowTableName = $"flow{GenerateWorkflowTablePrefix(workflowName)}flows";

            return QueryTable(workflowTableName, filter, select);
        }

        public static List<TableEntity> QueryMainTable(string filter, string[] select = null)
        {
            return QueryTable(DefinitionTableName, filter, select);
        }
    }
}
