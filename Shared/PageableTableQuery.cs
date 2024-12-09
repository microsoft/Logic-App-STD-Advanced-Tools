using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LogicAppAdvancedTool
{
    public class PageableTableQuery
    {
        private Pageable<TableEntity> pageableEntities;
        private string continuationToken;
        private int PageSize { get; set; }

        public bool HasNextPage { get; set; }

        public PageableTableQuery(string connectionString, string tableName, string query, string[] select = null, int pageSize = 1000)
        {
            if (pageSize > 1000)
            {
                Console.WriteLine($"Page size cannot be larger than 1000, change to 1000");
            }

            HasNextPage = true;
            PageSize = pageSize;

            TableServiceClient serviceClient = new TableServiceClient(AppSettings.ConnectionString);
            Pageable<TableItem> results = serviceClient.Query(filter: $"TableName eq '{tableName}'");

            if (results.Count() == 0)
            {
                throw new UserInputException($"Cannot find table named {tableName}, please review your input");
            }

            TableClient tableClient = new TableClient(connectionString, tableName);
            pageableEntities = tableClient.Query<TableEntity>(filter: query, select: select, maxPerPage: PageSize);
        }

        public List<TableEntity> GetNextPage()
        {
            IEnumerable<Page<TableEntity>> pages = pageableEntities.AsPages(continuationToken);
            Page<TableEntity> page = pages.ElementAt(0);

            continuationToken = page.ContinuationToken;

            if (continuationToken == null)
            {
                HasNextPage = false;
            }

            return page.Values.ToList();
        }
    }
}
