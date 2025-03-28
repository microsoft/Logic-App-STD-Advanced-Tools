using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Azure.Identity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LogicAppAdvancedTool
{
    public class TableOperations
    {
        public static string DefinitionTableName
        {
            get
            {
                return $"flow{StoragePrefixGenerator.GenerateLogicAppPrefix()}flows";
            }
        }

        public static TableClient GenerateTableClient(string tableName)
        {
            TableServiceClient tableServiceClient = StorageClientCreator.GenerateTableServiceClient();

            Pageable<TableItem> results = tableServiceClient.Query(filter: $"TableName eq '{tableName}'");

            if (results.Count() == 0)
            {
                throw new UserInputException($"Table - {tableName} not exist, please check whether parameters are correct or not.");
            }

            return tableServiceClient.GetTableClient(tableName);
        }

        private static List<TableEntity> QueryTable(string tableName, string query, string[] select = null)
        {
            TableClient tableClient = GenerateTableClient(tableName);

            return tableClient.Query<TableEntity>(filter: query, select: select).ToList();
        }

        public static List<TableEntity> QueryAccessKeyTable(string filter = null, string[] select = null)
        {
            string accessKeyTableName = $"flow{StoragePrefixGenerator.GenerateLogicAppPrefix()}flowaccesskeys";
            return QueryTable(accessKeyTableName, filter, select);
        }

        public static List<TableEntity> QueryHistoryTable(string workflowName, string filter = null, string[] select = null)
        {
            string historyTableName = $"flow{StoragePrefixGenerator.GenerateWorkflowTablePrefixByName(workflowName)}histories";

            return QueryTable(historyTableName, filter, select);
        }

        public static List<TableEntity> QueryRunTable(string workflowName, string filter, string[] select = null)
        {
            string runTableName = $"flow{StoragePrefixGenerator.GenerateWorkflowTablePrefixByName(workflowName)}runs";

            return QueryTable(runTableName, filter, select);
        }

        public static List<TableEntity> QueryRunTableByFlowID(string workflowID, string filter, string[] select = null)
        {
            string runTableName = $"flow{StoragePrefixGenerator.GenerateWorkflowTablePrefixByFlowID(workflowID)}runs";

            return QueryTable(runTableName, filter, select);
        }

        public static List<TableEntity> QueryActionTable(string workflowName, string date, string filter, string[] select = null)
        {
            string actionTableName = $"flow{StoragePrefixGenerator.GenerateWorkflowTablePrefixByName(workflowName)}{date}t000000zactions";

            return QueryTable(actionTableName, filter, select);
        }

        public static List<TableEntity> QueryActionTableByFlowID(string workflowID, string date, string filter, string[] select = null)
        {
            string actionTableName = $"flow{StoragePrefixGenerator.GenerateWorkflowTablePrefixByFlowID(workflowID)}{date}t000000zactions";

            return QueryTable(actionTableName, filter, select);
        }

        public static List<TableEntity> QueryWorkflowTable(string workflowName, string filter, string[] select = null)
        {
            string workflowTableName = $"flow{StoragePrefixGenerator.GenerateWorkflowTablePrefixByName(workflowName)}flows";

            return QueryTable(workflowTableName, filter, select);
        }

        public static List<TableEntity> QueryMainTable(string filter, string[] select = null)
        {
            return QueryTable(DefinitionTableName, filter, select);
        }

        public static List<TableEntity> QueryCurrentWorkflowByName(string workflowName, string[] select = null)
        {
            //Deleted workflows with same also saved in main table, so use FLOWLOOKUP to get current one
            string rowKey = $"MYEDGEENVIRONMENT_FLOWLOOKUP-MYEDGERESOURCEGROUP-{FormatRawKey(workflowName.ToUpper())}";

            return QueryMainTable($"RowKey eq '{rowKey}'", select);
        }

        public static List<TableEntity> QuerySubscriptionSummaryTable(string filter = null, string[] select = null)
        {
            string workflowTableName = $"flow{StoragePrefixGenerator.GenerateLogicAppPrefix()}flowsubscriptionsummary";

            return QueryTable(workflowTableName, filter, select);
        }

        public static string FormatRawKey(string rawKey)
        {
            return rawKey.Replace("_", ":5F").Replace("-", ":2D");
        }
    }
}
