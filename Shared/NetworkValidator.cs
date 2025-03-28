using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Files.Shares;
using Azure.Storage.Queues;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace LogicAppAdvancedTool
{
    public class DNSValidator
    {
        public string Endpoint { get; private set; }
        public IPAddress[] IPs { get; private set; }
        public ValidationStatus Result { get; private set; }

        public DNSValidator(string endpoint)
        {
            Result = ValidationStatus.NotApplicable;
            this.Endpoint = endpoint;
        }

        public DNSValidator Validate()
        {
            try
            {
                IPs = Dns.GetHostAddresses(Endpoint);

                Result = ValidationStatus.Succeeded;

            }
            catch
            {
                Result = ValidationStatus.Failed;
            }

            return this;
        }
    }

    public class SocketValidator
    {
        public string IP { get; private set; }
        public int Port { get; private set; }
        public ValidationStatus Result { get; private set; }

        public SocketValidator(IPAddress ip, int port)
        {
            Result = ValidationStatus.NotApplicable;
            this.IP = ip.ToString();
            this.Port = port;
        }

        public SocketValidator Validate()
        {
            Result = ValidationStatus.Failed;

            try
            {
                using (TcpClient client = new TcpClient())
                {
                    if (client.ConnectAsync(IP, Port).Wait(1000))
                    {
                        Result = ValidationStatus.Succeeded;
                    }
                    else
                    {
                        Result = ValidationStatus.Failed;
                    }
                };

            }
            catch
            {
                Result = ValidationStatus.Failed;
            }

            return this;
        }
    }

    public class SSLValidator
    {
        public string Endpoint { get; private set; }
        public ValidationStatus Result { get; private set; }

        private List<EventWrittenEventArgs> EventWrittenEvents;

        public SSLValidator(string endpoint)
        {
            Result = ValidationStatus.NotApplicable;
            this.Endpoint = endpoint;
            EventWrittenEvents = new List<EventWrittenEventArgs>();
        }

        public SSLValidator Validate()
        {
            SSLCertListener sslListener = new SSLCertListener();
            sslListener.EventWritten += SslListener_EventWritten;

            try
            {
                Result = ValidationStatus.Succeeded;

                HttpClient client = new HttpClient();
                HttpResponseMessage response = client.GetAsync(Endpoint).Result;
            }
            catch
            {
                Result = ValidationStatus.Failed;
            }
            finally
            {
                sslListener.EventWritten -= SslListener_EventWritten;
                sslListener.Dispose();
            }

            return this;
        }

        private void SslListener_EventWritten(object sender, EventWrittenEventArgs e)
        {
            foreach (string payload in e.Payload)
            {
                if (payload.Contains("RemoteCertificateNameMismatch"))
                {
                    Result = ValidationStatus.Failed;
                }
            }
        }

        public class SSLCertListener : EventListener
        {
            protected override void OnEventSourceCreated(EventSource eventSource)
            {
                if (eventSource.Name == "Private.InternalDiagnostics.System.Net.Http")
                {
                    EnableEvents(eventSource, EventLevel.LogAlways);
                }
            }

            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                if (eventData.EventName == "ErrorMessage")
                {
                    base.OnEventWritten(eventData);
                }
            }
        }
    }

    public class StorageValidator
    {
        public ValidationStatus Result { get; private set; }
        public string StorageEndpoint { get; private set; }
        public StorageServiceType ServiceType { get; private set; }

        public StorageValidator(string storageEndpoint, StorageServiceType serviceType)
        {
            this.StorageEndpoint = storageEndpoint;
            this.ServiceType = serviceType;
        }

        public StorageValidator Validate()
        {
            try
            {
                switch (ServiceType)
                {
                    case StorageServiceType.Blob:
                        BlobServiceClient blobClient = StorageClientCreator.GenerateBlobServiceClient();
                        blobClient.GetProperties();
                        break;
                    case StorageServiceType.File:
                        ShareServiceClient shareClient = new ShareServiceClient(AppSettings.FileShareConnectionString);  //file share only support for using connection string
                        shareClient.GetProperties();
                        break;
                    case StorageServiceType.Queue:
                        QueueServiceClient queueClient = StorageClientCreator.GenerateQueueServiceClient();
                        queueClient.GetProperties();
                        break;
                    case StorageServiceType.Table:
                        TableServiceClient tableClient = StorageClientCreator.GenerateTableServiceClient();
                        tableClient.GetProperties();
                        break;
                    default: break;
                }

                Result = ValidationStatus.Succeeded;
            }
            catch
            {
                Result = ValidationStatus.Failed;
            }

            return this;
        }
    }
}
