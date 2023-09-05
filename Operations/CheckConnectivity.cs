using Azure.Data.Tables;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Azure.Storage.Queues;
using Azure.Storage.Files.Shares;
using Azure.Storage.Blobs;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        private static void CheckConnectivity()
        {
            ConnectionInfo connectionInfo = new ConnectionInfo(AppSettings.ConnectionString);
            ConnectionValidator connectionValidator = new ConnectionValidator(connectionInfo);

            List<StorageValidationInfo> results = connectionValidator.Validate();

            if (results != null)
            {
                ConsoleTable consoleTable = new ConsoleTable("EndPoint", "Type", "DNS Test", "Endpoint IP", "TCP Port (443)", "Auth Status");

                foreach (StorageValidationInfo result in results)
                {
                    string endpoint = result.Endpoint;

                    consoleTable.AddRow(endpoint, result.ServiceType.ToString(), result.DNSStatus.ToString(), result.GetIPsAsString(), result.PingStatus.ToString(), result.AuthStatus.ToString());
                }

                consoleTable.Print();
            }
        }

        public class ConnectionValidator
        {
            private string LogicAppName;
            private ConnectionInfo ConnectionInfo;
            private List<StorageValidationInfo> Results;
            public ConnectionValidator(ConnectionInfo connectionInfo)
            {
                ConnectionInfo = connectionInfo;
                LogicAppName = AppSettings.LogicAppName;

                Results = new List<StorageValidationInfo>
                {
                    new StorageValidationInfo(connectionInfo.BlobEndpoint, StorageType.Blob),
                    new StorageValidationInfo(connectionInfo.FileEndpoint, StorageType.File),
                    new StorageValidationInfo(connectionInfo.QueueEndpoint, StorageType.Queue),
                    new StorageValidationInfo(connectionInfo.TableEndpoint, StorageType.Table)
                };
            }

            //temp resolution, need to improve in the future
            public List<StorageValidationInfo> Validate()
            {
                foreach (StorageValidationInfo info in Results)
                {
                    try
                    {
                        IPAddress[] ipAddressess = Dns.GetHostAddresses(info.Endpoint);

                        info.IPs = ipAddressess;
                        info.DNSStatus = ValidateStatus.Succeeded;
                    }
                    catch
                    {
                        info.DNSStatus = ValidateStatus.Failed;
                    }
                }

                foreach (StorageValidationInfo info in Results)
                {
                    if (info.DNSStatus != ValidateStatus.Succeeded)
                    {
                        continue;
                    }

                    try
                    {
                        foreach (IPAddress ip in info.IPs)
                        {
                            using (TcpClient client = new TcpClient(ip.ToString(), 443)) { };
                        }

                        info.PingStatus = ValidateStatus.Succeeded;

                    }
                    catch
                    {
                        info.PingStatus = ValidateStatus.Failed;
                    }
                }

                foreach (StorageValidationInfo info in Results)
                {
                    try
                    {
                        if (info.PingStatus != ValidateStatus.Succeeded)
                        {
                            continue;
                        }

                        switch (info.ServiceType)
                        {
                            case StorageType.Blob:
                                BlobServiceClient blobClient = new BlobServiceClient(AppSettings.ConnectionString);
                                blobClient.GetProperties();
                                break;
                            case StorageType.File:
                                ShareServiceClient shareClient = new ShareServiceClient(AppSettings.ConnectionString);
                                shareClient.GetProperties();
                                break;
                            case StorageType.Queue:
                                QueueServiceClient queueClient = new QueueServiceClient(AppSettings.ConnectionString);
                                queueClient.GetProperties();
                                break;
                            case StorageType.Table:
                                TableServiceClient tableClient = new TableServiceClient(AppSettings.ConnectionString);
                                tableClient.GetProperties();
                                break;
                            default: break;
                        }

                        info.AuthStatus = ValidateStatus.Succeeded;
                    }
                    catch
                    {
                        info.AuthStatus = ValidateStatus.Failed;
                    }
                }

                return Results;
            }
        }

        public enum StorageType
        {
            Blob = 1,
            File = 2,
            Queue = 4,
            Table = 8
        }
    }
}
