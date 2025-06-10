using Azure.Core.Pipeline;
using Azure.Identity;
using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http.Headers;

namespace LogicAppAdvancedTool
{
    internal partial class Tools
    {
        public static void BackupBundles(string storageAccount, string containerName)
        {
            CommonOperations.PromptConfirmation("This command requests \"Storage Blob Data Contributor\" role be assigned to Logic App system-assigned managed identity of your backup Storage Account.");

            Console.WriteLine($"Retrieving existing bundles from storage account: {storageAccount}, blob container: {containerName}");

            BlobClientOptions blobClientOptions = new BlobClientOptions()
            {
                RetryPolicy = new RetryPolicy(0)
            };

            BlobContainerClient blobContainerClient = StorageClientCreator.GenerateBlobClientBySystemMI(storageAccount, blobClientOptions).GetBlobContainerClient(containerName);
            List<string> existingBundles = blobContainerClient.GetBlobs()
                                            .Where(x => x.Name.EndsWith(".zip"))
                                            .Select(y => y.Name.Replace(".zip", ""))
                                            .ToList();

            Console.WriteLine($"Retrieved {existingBundles.Count} existing bundles in blob container");

            string bundleRootPath = "C:\\Program Files (x86)\\FuncExtensionBundles\\Microsoft.Azure.Functions.ExtensionBundle.Workflows";

            List<DirectoryInfo> bundlePaths = new DirectoryInfo(bundleRootPath).GetDirectories()
                                                                .Where( x=> !existingBundles.Contains(x.Name))
                                                                .ToList();

            if (bundlePaths.Count == 0)
            {
                Console.WriteLine("No new bundles found");

                return;
            }

            Console.WriteLine($"{bundlePaths.Count} new version bundle(s) detected.");

            foreach (DirectoryInfo bundlePath in bundlePaths)
            {
                string bundleVersion = bundlePath.Name;
                string targetTempFolder = $"bundles_temp\\{bundleVersion}";
                Console.WriteLine($"Copying bundle {bundleVersion} to local temp folder");
                CommonOperations.CopyDirectory(bundlePath.FullName, targetTempFolder, true);
                
                string zipFileName = $"{bundleVersion}.zip";
                string zipFilePath = $"bundles_temp\\{zipFileName}";

                if (File.Exists(zipFilePath))
                { 
                    File.Delete(zipFilePath);
                }

                Console.WriteLine($"Creating zip file for bundle {bundleVersion}, it will take ~1 minute");
                ZipFile.CreateFromDirectory(targetTempFolder, zipFilePath, CompressionLevel.NoCompression, false);

                Console.WriteLine($"Zip file has been created for bundle {bundleVersion}, uploading to blob container.");
                BlobClient blobClient = blobContainerClient.GetBlobClient(zipFileName);
                blobClient.Upload(zipFilePath);

                Console.WriteLine($"Bundle {bundleVersion} has been uploaded.");
            }

            Console.WriteLine("Cleaning up temp files.");
            Directory.Delete("bundles_temp", true);
            Console.WriteLine("Bundle(s) backup done.");
        }
    }
}