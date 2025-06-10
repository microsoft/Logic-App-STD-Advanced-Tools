using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LogicAppAdvancedTool
{
    public class StorageClientCreator
    {
        public static TableServiceClient GenerateTableServiceClient()
        {
            TableServiceClient tableServiceClient = null;

            if (!String.IsNullOrEmpty(AppSettings.ConnectionString))
            {
                tableServiceClient = new TableServiceClient(AppSettings.ConnectionString);
            }
            else
            {
                var credential = GenerateCredential("AzureWebJobsStorage__tableServiceUri");
 
                tableServiceClient = new TableServiceClient(new Uri(AppSettings.TableServiceUri), credential);
            }

            return tableServiceClient;
        }

        public static BlobServiceClient GenerateBlobServiceClient()
        {
            BlobServiceClient blobServiceClient = null;

            if (!String.IsNullOrEmpty(AppSettings.ConnectionString))
            {
                blobServiceClient = new BlobServiceClient(AppSettings.ConnectionString);
            }
            else
            {
                var credential = GenerateCredential("AzureWebJobsStorage__blobServiceUri");

                blobServiceClient = new BlobServiceClient(new Uri(AppSettings.BlobServiceUri), credential);
            }

            return blobServiceClient;
        }

        public static QueueServiceClient GenerateQueueServiceClient()
        {
            QueueServiceClient queueServiceClient = null;

            if (!String.IsNullOrEmpty(AppSettings.ConnectionString))
            {
                queueServiceClient = new QueueServiceClient(AppSettings.ConnectionString);
            }
            else
            {
                var credential = GenerateCredential("AzureWebJobsStorage__queueServiceUri");

                queueServiceClient = new QueueServiceClient(new Uri(AppSettings.QueueServiceUri), credential);
            }

            return queueServiceClient;
        }

        public static BlobServiceClient GenerateBlobClientBySystemMI(string storageAccount, BlobClientOptions clientOptions)
        {
#if !DEBUG
            DefaultAzureCredential credential = new DefaultAzureCredential();
            string blobUri = $"https://{storageAccount}.blob.core.windows.net";
            BlobServiceClient blobServiceClient = new BlobServiceClient(new Uri(blobUri), credential, clientOptions);

#else
            string connectionString = File.ReadAllText("Temp/ConnectionString.txt");
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString, clientOptions);
#endif

            return blobServiceClient;
        }

        private static DefaultAzureCredential GenerateCredential(string serviceUriName)
        {
            string serviceUri = Environment.GetEnvironmentVariable(serviceUriName);
            string miResourceID = AppSettings.ManagedIdentityResourceID;

            if (String.IsNullOrEmpty(serviceUri) || String.IsNullOrEmpty(miResourceID))
            {
                throw new ExpectedException($"Missing appsettings named \"AzureWebJobsStorage__managedIdentityResourceId\" or \"{serviceUriName}\"");
            }

            var credential = new DefaultAzureCredential(
                new DefaultAzureCredentialOptions
                {
                    ManagedIdentityResourceId = new Azure.Core.ResourceIdentifier(miResourceID)
                }
            );

            return credential;
        }
    }
}
