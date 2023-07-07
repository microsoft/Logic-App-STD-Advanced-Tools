using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.VisualBasic;
using Microsoft.WindowsAzure.ResourceStack.Common.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LogicAppAdvancedTool
{
    public partial class Program
    {
        public static List<TableEntity> QueryRunTable(string tableName, string query, string[] select = null)
        {
            List<TableEntity> tableEntities = new List<TableEntity>();

            TableServiceClient serviceClient = new TableServiceClient(AppSettings.ConnectionString);
            Pageable<TableItem> runTableItem = serviceClient.Query(filter: $"TableName eq '{tableName}'");

            if (runTableItem.Count() == 0)
            {
                throw new UserInputException($"Table - {tableName} not exist, please check whether parameters are correct or not.");
            }

            TableClient runTableClient = new TableClient(AppSettings.ConnectionString, tableName);
            tableEntities = runTableClient.Query<TableEntity>(filter: query, select: select).ToList();

            return tableEntities;
        }

        private static List<TableEntity> QueryActionTable(string tableName, string query)
        {
            List<TableEntity> tableEntities = new List<TableEntity>();

            TableServiceClient serviceClient = new TableServiceClient(AppSettings.ConnectionString);
            Pageable<TableItem> results = serviceClient.Query(filter: $"TableName eq '{tableName}'");

            if (results.Count() == 0)
            {
                throw new UserInputException($"Table - {tableName} not exist, please check whether parameters are correct or not.");
            }

            TableClient tableClient = new TableClient(AppSettings.ConnectionString, tableName);

            tableEntities = tableClient.Query<TableEntity>(filter: query).ToList();

            return tableEntities;
        }
    }
}
