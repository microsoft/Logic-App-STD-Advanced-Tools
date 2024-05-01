using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LogicAppAdvancedTool.Structures;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.WindowsAzure.ResourceStack.Common.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;

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

        public static string GenerateLogicAppPrefix()
        {
            return StoragePrefixGenerator.Generate(AppSettings.LogicAppName.ToLower());
        }

        public static string GenerateWorkflowTablePrefixByFlowID(string workflowID)
        {
            string logicAppPrefix = StoragePrefixGenerator.Generate(AppSettings.LogicAppName.ToLower());

            string workflowPrefix = StoragePrefixGenerator.Generate(workflowID.ToLower());

            return $"{logicAppPrefix}{workflowPrefix}";
        }

        public static string GenerateWorkflowTablePrefix(string workflowName)
        {
            //We may have multiple flow IDs with same workflow name, so use FLOWLOOKUP key to get current version of workflow
            List<TableEntity> tableEntities = TableOperations.QueryCurrentWorkflowByName(workflowName, new string[] { "FlowId" });

            if (tableEntities.Count() == 0)
            {
                throw new UserInputException($"{workflowName} cannot be found in storage table, please check whether workflow name is correct.");
            }

            string workflowID = tableEntities.First<TableEntity>().GetString("FlowId");

            return GenerateWorkflowTablePrefixByFlowID(workflowID);
        }

        public static List<string> ListFlowIDsByName(string workflowName)
        {
            List<string> ids = TableOperations.QueryMainTable($"FlowName eq '{workflowName}'", new string[] { "FlowId"})
                                            .Select(x => x.GetString("FlowId")) 
                                            .Distinct()
                                            .ToList();

            if (ids.Count() == 0)
            {
                throw new UserInputException($"{workflowName} cannot be found in storage table, please check whether workflow name is correct.");
            }

            return ids;
        }

        public static string BackupCurrentSite()
        {
            string filePath = $"{Directory.GetCurrentDirectory()}/Backup_{DateTime.Now.ToString("yyyyMMddHHmmss")}.zip";

            ZipFile.CreateFromDirectory(AppSettings.RootFolder, filePath, CompressionLevel.Fastest, false);

            return filePath;
        }

        #region Save worklfow definition from TableEntity
        public static void SaveDefinition(string path, string fileName, TableEntity entity)
        {
            byte[] definitionCompressed = entity.GetBinary("DefinitionCompressed");
            string kind = entity.GetString("Kind");
            string decompressedDefinition = DecompressContent(definitionCompressed);

            string fileContent = $"{{\"definition\": {decompressedDefinition},\"kind\": \"{kind}\"}}";

            dynamic jsonObject = JsonConvert.DeserializeObject(fileContent);
            string formattedContent = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);

            string filePath = $"{path}\\{fileName}";

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            File.WriteAllText(filePath, formattedContent);
        }
        #endregion

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
            StorageConnectionInfo info = new StorageConnectionInfo(AppSettings.ConnectionString, StorageServiceType.Blob);
            StorageSharedKeyCredential cred = new StorageSharedKeyCredential(info.AccountName, info.AccountKey);

            BlobClient client = new BlobClient(uri, cred);

            long blobSize = client.GetProperties().Value.ContentLength;

            if (contentSize != -1 && blobSize > contentSize)
            {
                string blobName = blobUri.Split("/").Last();

                //Console.WriteLine($"{blobName} content size is larger than 1MB, skip content check for this blob due to memory saving.");

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

        #region Create new workflow
        public static void CreateWorkflow(string workflowPath, string workflowName, string definition)
        {
            if (!Directory.Exists(workflowPath))
            {
                Directory.CreateDirectory(workflowPath);
            }

            File.WriteAllText($"{workflowPath}/workflow.json", definition);

            Console.WriteLine($"Workflow {workflowName} has been created in File Share, retrieving data from Storage Table...");

            List<TableEntity> workflowEntities = new List<TableEntity>();

            //After create an empty workflow, it might take several seconds to update Storage Table
            //try 10 times to retrieve newly create worklfow id
            for (int i = 1; i <= 10; i++)
            {
                workflowEntities = TableOperations.QueryMainTable($"FlowName eq '{workflowName}'");
                if (workflowEntities.Count != 0)
                {
                    break;
                }

                if (i == 10)
                {
                    throw new ExpectedException("Failed to retrieve records from Storage Table, please re-execute the command.");
                }

                Console.WriteLine($"Records not ingested into Storage Table yet, retry after 5 seconds, execution count {i}/10");
                Thread.Sleep(5000);
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

        public static bool IsIpInSubnet(string ip, string subnet)
        {
            string[] subnetInfo = subnet.Split('/');

            uint subnetStartIP = ConvertIPFromString(subnetInfo[0]);
            uint subnetEndIP = subnetStartIP;
            uint ipNum = ConvertIPFromString(ip);

            int subnetMask = int.Parse(subnetInfo.ElementAtOrDefault(1) ?? "32");

            //we don't need to consider for maximum value overflow
            //for subnet mask, the maximum value is 32, so uint value will be Pow(2, 32) which is 0 in Uint, but we have -1 which can revert back to Uint.Max
            subnetEndIP += (uint)Math.Pow(2, (32 - subnetMask)) - 1;

            return (ipNum >= subnetStartIP && ipNum <= subnetEndIP);
        }

        public static uint ConvertIPFromString(string IP)
        {
            byte[] IPBytes = IPAddress.Parse(IP).GetAddressBytes();
            uint IPNumber = (uint)IPBytes[0] << 24;
            IPNumber += (uint)IPBytes[1] << 16;
            IPNumber += (uint)IPBytes[2] << 8;
            IPNumber += IPBytes[3];

            return IPNumber;
        }
    }
}
