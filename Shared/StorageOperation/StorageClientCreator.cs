using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using System;
using System.Collections.Generic;
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
