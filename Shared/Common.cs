using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Microsoft.VisualBasic;
using Microsoft.WindowsAzure.ResourceStack.Common.Utilities;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace LogicAppAdvancedTool
{
    public partial class Program
    {
        /// <summary>
        /// Retrieve the table name which contains all the workflow definitions
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <returns></returns>
        private static string GetMainTableName(string LogicAppName)
        {
            string TableName = $"flow{StoragePrefixGenerator.Generate(LogicAppName.ToLower())}flows";

            TableServiceClient serviceClient = new TableServiceClient(ConnectionString);

            //Double check whether the table exists
            Pageable<TableItem> results = serviceClient.Query(filter: $"TableName eq '{TableName}'");

            if (results.Count() != 0)
            {
                return TableName;
            }

            throw new UserInputException("No table found in Azure Storage Account, please check whether the Logic App Name correct or not.");
        }

        /// <summary>
        /// Retrieve the table name which contains all the workflow definitions
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <returns></returns>
        private static string GetMainTablePrefix(string LogicAppName)
        {
            string TablePrefix = StoragePrefixGenerator.Generate(LogicAppName.ToLower());
            string TableName = $"flow{TablePrefix}flows";

            TableServiceClient serviceClient = new TableServiceClient(ConnectionString);

            //Double check whether the table exists
            Pageable<TableItem> results = serviceClient.Query(filter: $"TableName eq '{TableName}'");

            if (results.Count() != 0)
            {
                return TablePrefix;
            }

            Console.WriteLine("No table found in Azure Storage Account, please check whether the Logic App Name correct or not.");
            return string.Empty;
        }

        private static string GenerateWorkflowTablePrefix(string LogicAppName, string WorkflowName)
        {
            string mainTableName = GetMainTableName(LogicAppName);

            TableClient tableClient = new TableClient(ConnectionString, mainTableName);
            Pageable<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: $"FlowName eq '{WorkflowName}'");

            if (tableEntities.Count() == 0)
            {
                throw new UserInputException($"{WorkflowName} cannot be found in storage table, please check whether workflow name is correct.");
            }

            string logicAppPrefix = StoragePrefixGenerator.Generate(LogicAppName.ToLower());

            string workflowID = tableEntities.First<TableEntity>().GetString("FlowId");
            string workflowPrefix = StoragePrefixGenerator.Generate(workflowID.ToLower());

            return $"{logicAppPrefix}{workflowPrefix}";
        }

        private static string BackupCurrentSite()
        {
            string FilePath = $"{Directory.GetCurrentDirectory()}/Backup_{DateTime.Now.ToString("yyyyMMddHHmmss")}.zip";

            ZipFile.CreateFromDirectory("C:/home/site/wwwroot/", FilePath, CompressionLevel.Fastest, false);

            return FilePath;
        }

        public static string ConnectionString
        {
            get
            {
                return Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            }
        }

        /// <summary>
        /// Decompress the content which compressed by Inflate
        /// </summary>
        /// <param name="Content"></param>
        /// <returns></returns>
        public static string DecompressContent(byte[] Content)
        {
            if (Content == null)
            {
                return null;
            }

            string Result = DeflateCompressionUtility.Instance.InflateString(new MemoryStream(Content));

            return Result;
        }

        public static byte[] CompressContent(string Content)
        {
            if (string.IsNullOrEmpty(Content))
            {
                return null;
            }

            MemoryStream CompressedStream = DeflateCompressionUtility.Instance.DeflateString(Content);
            byte[] CompressedBytes = CompressedStream.ToArray();

            return CompressedBytes;
        }

        public class WorkflowDefinition
        {
            public string WorkflowName;
            public string Version;
            public string ModifiedData;
            public string Definition;

            public WorkflowDefinition(string WorkflowName, string Version, string ModifiedData, string Definition)
            {
                this.WorkflowName = WorkflowName;
                this.Version = Version;
                this.ModifiedData = ModifiedData;
                this.Definition = Definition;
            }
        }

        public class WorkflowTemplate
        {
            public object definition { get; set; }
            public string kind { get; set; }
        }

        public class UserInputException : Exception 
        {     
            public UserInputException(string Message) : base(Message) { }
        }
    }
}
