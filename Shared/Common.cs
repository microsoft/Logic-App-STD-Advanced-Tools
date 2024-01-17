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

        public static List<string> RetrieveServiceTagIPs(string serviceTag)
        {
            MSIToken token = MSITokenService.RetrieveToken("https://management.azure.com");
            string serviceTagUrl = $"https://management.azure.com/subscriptions/{AppSettings.SubscriptionID}/providers/Microsoft.Network/locations/{AppSettings.Region.ToLower()}/serviceTags?api-version=2023-06-01";
            string serviceTagResponse = HttpOperations.ValidatedHttpRequestWithToken(serviceTagUrl, HttpMethod.Get, null, token.access_token, "Cannot grab Azure Connector IP range from Internet");

            JToken serviceTagInfo = JObject.Parse(serviceTagResponse)["values"].ToList()
                                    .Where(s => s["name"].ToString() == $"{serviceTag}")
                                    .FirstOrDefault();

            List<string> ipPrefixes = serviceTagInfo["properties"]?["addressPrefixes"].ToList()
                                        .Select(s => s.ToString())
                                        .Where(s => !s.Contains(":"))   //we don't need IPv6
                                        .ToList();

            return ipPrefixes;
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
