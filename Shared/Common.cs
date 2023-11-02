using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LogicAppAdvancedTool.Structures;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.WindowsAzure.ResourceStack.Common.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace LogicAppAdvancedTool
{
    public static class CommonOperations
    {
        public static void AlertExperimentalFeature()
        {
            string confirmationMessage = "IMPORTANT!!! This is an experimental feature which might cause unexpected behavior (environment crash, data lossing,etc) in your Logic App.\r\nInput for confirmation to execute:";
            if (!Prompt.GetYesNo(confirmationMessage, false, ConsoleColor.Red))
            {
                throw new UserCanceledException("Operation Cancelled");
            }
        }
        private static string GetMainTableName()
        {
            string tableName = $"flow{StoragePrefixGenerator.Generate(AppSettings.LogicAppName.ToLower())}flows";

            TableServiceClient serviceClient = new TableServiceClient(AppSettings.ConnectionString);

            //Double check whether the table exists
            Pageable<TableItem> results = serviceClient.Query(filter: $"TableName eq '{tableName}'");

            if (results.Count() != 0)
            {
                return tableName;
            }

            throw new UserInputException("No table found in Azure Storage Account, please check whether the Logic App Name correct or not.");
        }

        public static string GenerateLogicAppPrefix()
        {
            return StoragePrefixGenerator.Generate(AppSettings.LogicAppName.ToLower());
        }

        public static string GenerateWorkflowTablePrefix(string workflowName)
        {
            string mainTableName = GetMainTableName();

            TableClient tableClient = new TableClient(AppSettings.ConnectionString, mainTableName);
            Pageable<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: $"FlowName eq '{workflowName}'");

            if (tableEntities.Count() == 0)
            {
                throw new UserInputException($"{workflowName} cannot be found in storage table, please check whether workflow name is correct.");
            }

            string logicAppPrefix = StoragePrefixGenerator.Generate(AppSettings.LogicAppName.ToLower());

            string workflowID = tableEntities.First<TableEntity>().GetString("FlowId");
            string workflowPrefix = StoragePrefixGenerator.Generate(workflowID.ToLower());

            return $"{logicAppPrefix}{workflowPrefix}";
        }

        public static string BackupCurrentSite()
        {
            string filePath = $"{Directory.GetCurrentDirectory()}/Backup_{DateTime.Now.ToString("yyyyMMddHHmmss")}.zip";

            ZipFile.CreateFromDirectory(AppSettings.RootFolder, filePath, CompressionLevel.Fastest, false);

            return filePath;
        }

        #region Deflate/infalte related
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
        #endregion

        #region Storage operation
        public static string GetBlobContent(string blobUri, int contentSize = -1)
        {
            Uri uri = new Uri(blobUri);
            StorageConnectionInfo info = new StorageConnectionInfo(AppSettings.ConnectionString);
            StorageSharedKeyCredential cred = new StorageSharedKeyCredential(info.AccountName, info.AccountKey);

            BlobClient client = new BlobClient(uri, cred);

            long blobSize = client.GetProperties().Value.ContentLength;

            if (contentSize != -1 && blobSize > contentSize)
            {
                string blobName = blobUri.Split("/").Last();

                Console.WriteLine($"{blobName} content size is larger than 1MB, skip content check for this blob due to memory saving.");

                return String.Empty;
            }

            BlobDownloadResult result = client.DownloadContent().Value;
            Stream contentStream = result.Content.ToStream();

            using (BinaryReader br = new BinaryReader(contentStream))
            {
                byte[] b = br.ReadBytes((int)contentStream.Length);

                return DecompressContent(b);
            }
        }
        #endregion

        #region Get embdded resource
        public static string GetEmbeddedResource(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] s = assembly.GetManifestResourceNames();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader sr = new StreamReader(stream))
            {
                return sr.ReadToEnd();
            }
        }
        #endregion

        public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            Directory.CreateDirectory(destinationDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }
    }
}
