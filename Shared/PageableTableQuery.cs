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

        public bool HasNextPage { get; set; }

        public PageableTableQuery(string connectionString, string tableName, string query, string[] select = null)
        {
            HasNextPage = true;

            TableClient tableClient = new TableClient(connectionString, tableName);
            pageableEntities = tableClient.Query<TableEntity>(filter: query, select: select, maxPerPage:1000);
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
