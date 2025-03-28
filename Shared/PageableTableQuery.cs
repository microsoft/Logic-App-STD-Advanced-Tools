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

        public bool HasNextPage { get; private set; }

        public PageableTableQuery(string tableName, string query, string[] select = null, int pageSize = 1000)
        {
            if (pageSize > 1000)
            {
                pageSize = 1000;
                Console.WriteLine($"Page size cannot be larger than 1000, change to 1000 for memory consideration.");
            }

            HasNextPage = true;
            PageSize = pageSize;

            TableClient tableClient = TableOperations.GenerateTableClient(tableName);
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
