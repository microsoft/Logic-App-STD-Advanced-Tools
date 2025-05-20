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

            BlobContainerClient blobContainerClient = CreateBlobClient(storageAccount, containerName);
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

            Directory.Delete("bundles_temp", true);
            Console.WriteLine("Temp folder has been cleaned up, bundle(s) backup done.");
        }

        private static BlobContainerClient CreateBlobClient(string storageAccount, string containerName)
        {
#if !DEBUG
            /*
            DefaultAzureCredential credential = new DefaultAzureCredential(
                new DefaultAzureCredentialOptions
                {
                    //ManagedIdentityResourceId = new Azure.Core.ResourceIdentifier(AppSettings.ManagedIdentityResourceID)
                    ManagedIdentityClientId = "d99a7597-a635-448f-8941-09779c79c02f"
                });
            */

            DefaultAzureCredential credential = new DefaultAzureCredential();
            string blobUri = $"https://{storageAccount}.blob.core.windows.net";
            BlobServiceClient blobServiceClient = new BlobServiceClient(new Uri(blobUri), credential);

#else
            string connectionString = File.ReadAllText("Temp/ConnectionString.txt");
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
#endif
            BlobContainerClient blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

            return blobContainerClient;
        }
    }
}