using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LogicAppAdvancedTool
{
    public class TableQueryWithPagination
    {
        public class PageableTableEntity
        {
            private string ConnectionString;

            private string TableName;
            private Pageable<TableEntity> Entities;

            public PageableTableEntity(string connectionString, string tableName)
            {
                ConnectionString = connectionString;
                TableName = tableName;

                Entities = new TableClient(connectionString, tableName).Query<TableEntity>(maxPerPage: 1000);
            }
        }
    }
}
