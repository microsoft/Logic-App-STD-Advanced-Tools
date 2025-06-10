using Azure;
using Azure.Core.Pipeline;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
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
        public static void RollbackBundle(string storageAccount, string containerName, string blobName, string bundleVersion, bool anonymous)
        {
            BlockBlobClient blobClient = null;
            string blobPath = $"https://{storageAccount}.blob.core.windows.net/{containerName}/{blobName}";

            BlobClientOptions blobClientOptions = new BlobClientOptions()
            {
                RetryPolicy = new RetryPolicy(0)
            };

            if (anonymous)
            {
                blobClient = new BlockBlobClient(new Uri(blobPath));
            }
            else
            {
                CommonOperations.PromptConfirmation("This command requests \"Storage Blob Data Reader\" role be assigned to Logic App system-assigned managed identity of your Storage Account.");
                BlobContainerClient blobContainerClient = StorageClientCreator.GenerateBlobClientBySystemMI(storageAccount, blobClientOptions).GetBlobContainerClient(containerName);
                blobClient = blobContainerClient.GetBlockBlobClient(blobName);
            }

            try
            {
                blobClient.GetProperties();
            }
            catch (RequestFailedException ex) 
            {
                throw new UserInputException($"Error: {ex.ErrorCode ?? ex.Message}, Failed to retrieve {blobPath}");
            }

            Console.WriteLine($"Downloading bundle to local folder, it will take ~2 minutes");
            blobClient.DownloadTo(blobName);
            Console.WriteLine($"Bundle has been downloaded, extract to bundle folder");

            string bundleDirectory = $"C:/home/data/Functions/ExtensionBundles/Microsoft.Azure.Functions.ExtensionBundle.Workflows/{bundleVersion}";
            if (!Directory.Exists(bundleDirectory))
            { 
                Directory.CreateDirectory(bundleDirectory);
            }

            ZipFile.ExtractToDirectory(blobName, bundleDirectory);
            Console.WriteLine($"Bundle has been extracted in local bundle folder, please modify appsetting \"AzureFunctionsJobHost__extensionBundle__version\" = \"{bundleVersion}\"");
        }
    }
}