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
        private static string GetMainTableName(string logicAppName)
        {
            string tableName = $"flow{StoragePrefixGenerator.Generate(logicAppName.ToLower())}flows";

            TableServiceClient serviceClient = new TableServiceClient(AppSettings.ConnectionString);

            //Double check whether the table exists
            Pageable<TableItem> results = serviceClient.Query(filter: $"TableName eq '{tableName}'");

            if (results.Count() != 0)
            {
                return tableName;
            }

            throw new UserInputException("No table found in Azure Storage Account, please check whether the Logic App Name correct or not.");
        }

        /// <summary>
        /// Retrieve the table name which contains all the workflow definitions
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <returns></returns>
        private static string GetMainTablePrefix(string logicAppName)
        {
            string tablePrefix = StoragePrefixGenerator.Generate(logicAppName.ToLower());
            string tableName = $"flow{tablePrefix}flows";

            TableServiceClient serviceClient = new TableServiceClient(AppSettings.ConnectionString);

            //Double check whether the table exists
            Pageable<TableItem> results = serviceClient.Query(filter: $"TableName eq '{tableName}'");

            if (results.Count() != 0)
            {
                return tablePrefix;
            }

            Console.WriteLine("No table found in Azure Storage Account, please check whether the Logic App Name correct or not.");
            return string.Empty;
        }

        private static string GenerateLogicAppPrefix(string logicAppName) 
        {
            string tablePrefix = StoragePrefixGenerator.Generate(logicAppName.ToLower());

            return tablePrefix;
        }

        /// <summary>
        /// Generate workflow table prefix in Storage Table
        /// </summary>
        /// <param name="logicAppName"></param>
        /// <param name="workflowName"></param>
        /// <returns></returns>
        /// <exception cref="UserInputException"></exception>
        private static string GenerateWorkflowTablePrefix(string logicAppName, string workflowName)
        {
            string mainTableName = GetMainTableName(logicAppName);

            TableClient tableClient = new TableClient(AppSettings.ConnectionString, mainTableName);
            Pageable<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: $"FlowName eq '{workflowName}'");

            if (tableEntities.Count() == 0)
            {
                throw new UserInputException($"{workflowName} cannot be found in storage table, please check whether workflow name is correct.");
            }

            string logicAppPrefix = StoragePrefixGenerator.Generate(logicAppName.ToLower());

            string workflowID = tableEntities.First<TableEntity>().GetString("FlowId");
            string workflowPrefix = StoragePrefixGenerator.Generate(workflowID.ToLower());

            return $"{logicAppPrefix}{workflowPrefix}";
        }

        private static string BackupCurrentSite()
        {
            string filePath = $"{Directory.GetCurrentDirectory()}/Backup_{DateTime.Now.ToString("yyyyMMddHHmmss")}.zip";

            ZipFile.CreateFromDirectory("C:/home/site/wwwroot/", filePath, CompressionLevel.Fastest, false);

            return filePath;
        }

        /// <summary>
        /// Decode run history actions input/output content
        /// </summary>
        /// <param name="binaryContent"></param>
        /// <returns></returns>
        public static dynamic DecodeActionPayload(byte[] binaryContent)
        {
            string rawContent = DecompressContent(binaryContent);

            if (rawContent == null)
            {
                return null;
            }

            //Recently there are 2 different JSON schema for output payload, try connector schema first
            ConnectorPayloadStructure connectorPayload = JsonConvert.DeserializeObject<ConnectorPayloadStructure>(rawContent);

            dynamic output = null;

            if (connectorPayload.ContentLinks != null)
            {
                string inlineContent = connectorPayload.ContentLinks.Body.InlinedContent;
                output = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(Convert.FromBase64String(inlineContent)));
                return output;
            }

            CommonPayloadStructure payload = JsonConvert.DeserializeObject<CommonPayloadStructure>(rawContent);
            output = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(Convert.FromBase64String(payload.InlinedContent)));

            return output;
        }

        /// <summary>
        /// Decompress the content which compressed by Inflate
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static string DecompressContent(byte[] content)
        {
            if (content == null)
            {
                return null;
            }

            string result = DeflateCompressionUtility.Instance.InflateString(new MemoryStream(content));

            return result;
        }

        /// <summary>
        /// Compress string to Inflate stream
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static byte[] CompressContent(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return null;
            }

            MemoryStream compressedStream = DeflateCompressionUtility.Instance.DeflateString(content);
            byte[] compressedBytes = compressedStream.ToArray();

            return compressedBytes;
        }
    }
}
