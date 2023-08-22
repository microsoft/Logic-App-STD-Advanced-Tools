using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Files.Shares;
using Azure.Storage.Queues;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace LogicAppAdvancedTool
{
    public partial class Program
    {
        public class ServiceProviderValidator
        {
            public string Name { get; set; }
            public string DisplayName { get; set; }
            public ServiceProviderType ServiceProvider { get; set; }
            public string Endpoint { get; set; }
            public int Port { get; set; }
            public IPAddress IP { get; private set; }
            public bool IsIPAddress { get; private set; }
            public bool IsEndpointEmpty { get; private set; }
            public ValidationInfo Result { get; private set; }

            public string DNSTestResult { get; private set; }
            public string ConnectionTestResult { get; private set; }

            public ServiceProviderValidator(string Name, string DisplayName, ServiceProviderType ServiceProvider, string Endpoint, int Port)
            {
                this.Name = Name;
                this.DisplayName = DisplayName;
                this.ServiceProvider = ServiceProvider;
                this.Endpoint = Endpoint;
                this.Port = Port;

                IPAddress ip;
                IsIPAddress = IPAddress.TryParse(Endpoint, out ip);

                if (IsIPAddress)
                {
                    this.IP = ip;
                }

                IsEndpointEmpty = String.IsNullOrEmpty(Endpoint);
            }

            public void Validate()
            {
                if (IsEndpointEmpty)
                {
                    DNSTestResult = "No Endpoint found";
                    ConnectionTestResult = "No Endpoint found";


                    return;
                }

                Result = new ValidationInfo(Endpoint);

                if (!IsIPAddress)
                {
                    try
                    {
                        IPAddress[] ipAddressess = Dns.GetHostAddresses(Result.Endpoint);

                        IP = ipAddressess[0];   //assume only 1 IP will return
                        Result.DNSStatus = ValidateStatus.Succeeded;

                        DNSTestResult = "Passed";
                    }
                    catch
                    {
                        Result.DNSStatus = ValidateStatus.Failed;
                        DNSTestResult = "Failed";
                        ConnectionTestResult = "N/A (DNS test failed)";

                        return;
                    }
                }
                else
                {
                    DNSTestResult = "N/A (Only IP)";
                }


                try
                {
                    TcpClient tcpClient = new TcpClient();
                    if (tcpClient.ConnectAsync(IP.ToString(), Port).Wait(1000))
                    {
                        Result.PingStatus = ValidateStatus.Succeeded;

                        ConnectionTestResult = "Passed";
                    }
                    else
                    {
                        Result.PingStatus = ValidateStatus.Failed;

                        ConnectionTestResult = "Failed";
                    }
                }
                catch (Exception ex)
                {
                    ConnectionTestResult = "Failed";
                }
                
            }
        }

        public enum ServiceAuthProvider
        {
            ActiveDirectoryOAuth,
            ManagedServiceIdentity,
            connectionString,
            accessKey,
            None
        }

        public enum ServiceProviderType
        {
            AzureBlob,
            AzureCosmosDB,
            AzureFile,
            DB2,
            Ftp,
            Sftp,
            Smtp,
            azureTables,
            azurequeues,
            eventGridPublisher,
            eventHub,
            keyVault,
            mq,
            serviceBus,
            sql
        }
    }
}