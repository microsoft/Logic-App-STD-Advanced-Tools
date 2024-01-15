﻿using Azure.Data.Tables;
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
            Console.WriteLine("Validating...");

            StorageConnectionInfo connectionInfo = new StorageConnectionInfo(AppSettings.ConnectionString);

            List<BackendStorageValidator> results = new List<BackendStorageValidator>
                {
                    new BackendStorageValidator(connectionInfo.BlobEndpoint, StorageType.Blob),
                    new BackendStorageValidator(connectionInfo.FileEndpoint, StorageType.File),
                    new BackendStorageValidator(connectionInfo.QueueEndpoint, StorageType.Queue),
                    new BackendStorageValidator(connectionInfo.TableEndpoint, StorageType.Table)
                };

            List<string> storagePublicIPs = null;
            try
            {
                storagePublicIPs = CommonOperations.RetrieveServiceTagIPs("Storage");

                Console.WriteLine("IP list of Storage Account service tag has been retrieved successfully.");
            }
            catch
            {
                Console.WriteLine("Failed to retrieve Storage Service Tag IP lists, please review whether your Logic App MI has been assign \"Reader\" role on subscription level.\r\nStorage Account private endpoint validation will be skipped.");
            }

            foreach (BackendStorageValidator validator in results)
            {
                validator.Validate();

                if (storagePublicIPs != null && validator.NameResolutionStatus == ValidationStatus.Succeeded)
                {
                    validator.CheckForPE(storagePublicIPs);
                }
            }

            if (results != null)
            {
                ConsoleTable consoleTable = new ConsoleTable("Storage Name", "Type", "DNS Resolution", "Endpoint IP", "Is PE", "TCP Conn", "Authentication");

                foreach (BackendStorageValidator result in results)
                {
                    consoleTable.AddRow(connectionInfo.AccountName, result.ServiceType.ToString(), result.NameResolutionStatus.ToString(), result.GetIPsAsString(), result.IsPrivateEndpoint, result.SocketConnectionStatus.ToString(), result.AuthenticationStatus.ToString());
                }

                consoleTable.Print();
            }
        }
    }

    public class BackendStorageValidator
    {
        public string Endpoint;
        public StorageType ServiceType;
        public int Port;
        public IPAddress[] IPs;

        public ValidationStatus AuthenticationStatus { get; private set; }
        public ValidationStatus NameResolutionStatus { get; private set; }
        public ValidationStatus SocketConnectionStatus { get; private set; }
        public string IsPrivateEndpoint { get; private set; }

        public BackendStorageValidator(string endpoint, StorageType serviceType)
        {
            Endpoint = endpoint;
            Port = 443;
            ServiceType = serviceType;
            AuthenticationStatus = ValidationStatus.NotApplicable;
            IsPrivateEndpoint = "Skipped";
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
                if (CommonOperations.VerifyIPInSubnet(IPs[0].ToString(), subnet))
                {
                    IsPrivateEndpoint = "Yes";

                    break;
                }
            }

            IsPrivateEndpoint = "No";
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
