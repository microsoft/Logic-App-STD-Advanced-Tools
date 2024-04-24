using Azure.Data.Tables;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Azure.Storage.Queues;
using Azure.Storage.Files.Shares;
using Azure.Storage.Blobs;
using LogicAppAdvancedTool.Structures;
using System;

namespace LogicAppAdvancedTool.Operations
{
    public static class ValidateStorageConnectivity
    {
        public static void Run()
        {
            List<BackendStorageValidator> validators = new List<BackendStorageValidator>
                {
                    new BackendStorageValidator(new StorageConnectionInfo(AppSettings.ConnectionString, StorageServiceType.Blob)),
                    new BackendStorageValidator(new StorageConnectionInfo(AppSettings.ConnectionString, StorageServiceType.Queue)),
                    new BackendStorageValidator(new StorageConnectionInfo(AppSettings.ConnectionString, StorageServiceType.Table))
                };

            if (AppSettings.FileShareConnectionString != null)
            {
                validators.Add(new BackendStorageValidator(new StorageConnectionInfo(AppSettings.FileShareConnectionString, StorageServiceType.File)));
                Console.WriteLine($"Successfully retrieved Storage Account information from environment variables.");
            }
            else
            {
                Console.WriteLine($"Cannot retrieve connection string of Storage - File Share, validation will be skipped for file share service.\r\nIf you are NOT using ASEv3, please verify WEBSITE_CONTENTAZUREFILECONNECTIONSTRING in your appsettings.");
            }


            List<string> storagePublicIPs = null;

            try
            {
                ServiceTagRetriever serviceTagRetriever = new ServiceTagRetriever();
                storagePublicIPs = serviceTagRetriever.GetIPsV4ByName("Storage");

                Console.WriteLine("IP list of Storage Account service tag has been retrieved successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to fetch service tag of Storage, ignore public/private IP validation");
            }

            foreach (BackendStorageValidator validator in validators)
            {
                validator.Validate();

                if (storagePublicIPs != null && validator.NameResolutionStatus == ValidationStatus.Succeeded)
                {
                    validator.CheckForPE(storagePublicIPs);
                }
            }

            if (validators != null)
            {
                ConsoleTable consoleTable = new ConsoleTable("Storage Name", "Type", "DNS Resolution", "Endpoint IP", "Is PE", "TCP Conn", "Authentication");

                foreach (BackendStorageValidator result in validators)
                {
                    consoleTable.AddRow(result.connectionInfo.AccountName, result.ServiceType.ToString(), result.NameResolutionStatus.ToString(), result.GetIPsAsString(), result.IsPrivateEndpoint.ToString(), result.SocketConnectionStatus.ToString(), result.AuthenticationStatus.ToString());
                }

                consoleTable.Print();
            }
        }
    }

    public class BackendStorageValidator
    {
        public string Endpoint { get; private set; }
        public StorageServiceType ServiceType { get; private set; }
        public int Port { get; private set; }
        public IPAddress[] IPs { get; private set; }
        public StorageConnectionInfo connectionInfo { get; private set; }
        public ValidationStatus AuthenticationStatus { get; private set; }
        public ValidationStatus NameResolutionStatus { get; private set; }
        public ValidationStatus SocketConnectionStatus { get; private set; }
        public PrivateEndpointStatus IsPrivateEndpoint { get; private set; }

        public BackendStorageValidator(StorageConnectionInfo connectionInfo)
        {
            Endpoint = connectionInfo.Endpoint;
            Port = 443;
            ServiceType = connectionInfo.storageType;
            AuthenticationStatus = ValidationStatus.NotApplicable;
            IsPrivateEndpoint = PrivateEndpointStatus.Skipped;
            this.connectionInfo = connectionInfo;
        }

        public void Validate()
        {
            DNSValidator dnsValidator = new DNSValidator(Endpoint).Validate();
            NameResolutionStatus = dnsValidator.Result;

            IPs = dnsValidator.IPs;

            if (NameResolutionStatus == ValidationStatus.Succeeded)
            {
                foreach (IPAddress ip in IPs)
                {
                    SocketConnectionStatus = new SocketValidator(ip, Port).Validate().Result;
                }
            }

            if (SocketConnectionStatus == ValidationStatus.Succeeded)
            {
                AuthenticationStatus = new StorageValidator(Endpoint, ServiceType).Validate().Result;
            }
        }

        public void CheckForPE(List<string> serviceSubnets)
        {
            foreach (string subnet in serviceSubnets)
            {
                if (CommonOperations.IsIpInSubnet(IPs[0].ToString(), subnet))       //assume for each Storage Account endpoint, only 1 IP
                {
                    IsPrivateEndpoint = PrivateEndpointStatus.Yes;

                    break;
                }
            }

            IsPrivateEndpoint = PrivateEndpointStatus.No;
        }

        public string GetIPsAsString()
        {
            if (IPs != null)
            {
                string IPsAsString = "";
                foreach (IPAddress ip in IPs)
                {
                    IPsAsString += ip.ToString() + " ";
                }

                return IPsAsString.TrimEnd();
            }

            return "N/A";
        }
    }
}
