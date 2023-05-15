using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Microsoft.VisualBasic;
using Microsoft.WindowsAzure.ResourceStack.Common.Utilities;
using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

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

            TableServiceClient serviceClient = new TableServiceClient(connectionString);

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

            TableServiceClient serviceClient = new TableServiceClient(connectionString);

            //Double check whether the table exists
            Pageable<TableItem> results = serviceClient.Query(filter: $"TableName eq '{TableName}'");

            if (results.Count() != 0)
            {
                return TablePrefix;
            }

            Console.WriteLine("No table found in Azure Storage Account, please check whether the Logic App Name correct or not.");
            return string.Empty;
        }

        private static string GenerateLogicAppPrefix(string LogicAppName) 
        {
            string TablePrefix = StoragePrefixGenerator.Generate(LogicAppName.ToLower());

            return TablePrefix;
        }

        /// <summary>
        /// Generate workflow table prefix in Storage Table
        /// </summary>
        /// <param name="LogicAppName"></param>
        /// <param name="WorkflowName"></param>
        /// <returns></returns>
        /// <exception cref="UserInputException"></exception>
        private static string GenerateWorkflowTablePrefix(string LogicAppName, string WorkflowName)
        {
            string mainTableName = GetMainTableName(LogicAppName);

            TableClient tableClient = new TableClient(connectionString, mainTableName);
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

        public static string connectionString
        {
            get
            {
                return Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            }
        }

        /// <summary>
        /// Decode run history actions input/output content
        /// </summary>
        /// <param name="BinaryContent"></param>
        /// <returns></returns>
        public static dynamic DecodeActionPayload(byte[] BinaryContent)
        {
            string RawContent = DecompressContent(BinaryContent);

            if (RawContent == null)
            {
                return null;
            }

            //Recently there are 2 different JSON schema for output payload, try connector schema first
            ConnectorPayloadStructure ConnectorPayload = JsonConvert.DeserializeObject<ConnectorPayloadStructure>(RawContent);

            dynamic Output = null;

            if (ConnectorPayload.nestedContentLinks != null)
            {
                string inlineContent = ConnectorPayload.nestedContentLinks.body.inlinedContent;
                Output = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(Convert.FromBase64String(inlineContent)));
                return Output;
            }

            CommonPayloadStructure Payload = JsonConvert.DeserializeObject<CommonPayloadStructure>(RawContent);
            Output = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(Convert.FromBase64String(Payload.inlinedContent)));

            return Output;
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

        /// <summary>
        /// Compress string to Inflate stream
        /// </summary>
        /// <param name="Content"></param>
        /// <returns></returns>
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
    }
}
