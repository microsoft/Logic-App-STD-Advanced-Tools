using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace LAVersionReverter
{
    public partial class Program
    {
        /// <summary>
        /// Retrieve the table name which contains all the workflow definitions
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <returns></returns>
        private static string GetMainTableName(string LogicAppName, string ConnectionString)
        {
            string TableName = $"flow{StoragePrefixGenerator.Generate(LogicAppName.ToLower())}flows";

            TableServiceClient serviceClient = new TableServiceClient(ConnectionString);

            //Double check whether the table exists
            Pageable<TableItem> results = serviceClient.Query(filter: $"TableName eq '{TableName}'");

            if (results.Count() != 0)
            {
                return TableName;
            }

            Console.WriteLine("No table found in Azure Storage Account, please check whether the Logic App Name correct or not.");
            return string.Empty;
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

            string Result = String.Empty;

            MemoryStream output = new MemoryStream();

            using (var compressStream = new MemoryStream(Content))
            {
                using (var decompressor = new DeflateStream(compressStream, CompressionMode.Decompress))
                {
                    decompressor.CopyTo(output);
                }
                output.Position = 0;
            }

            using (StreamReader reader = new StreamReader(output))
            {
                Result = reader.ReadToEnd();
            }

            return Result;
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
    }
}
