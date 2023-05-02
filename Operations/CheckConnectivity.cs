using Azure;
using Azure.Data.Tables;
using System;
using Azure.Storage.Sas;
using System.IO;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using System.Net.Sockets;
using Azure.Storage.Queues;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        private static void CheckConnectivity(string LogicAppName)
        {
            string FileShareName = Environment.GetEnvironmentVariable("WEBSITE_CONTENTSHARE");

            ConnectionInfo connectionInfo = new ConnectionInfo(ConnectionString);
            ConnectionValidator connectionValidator = new ConnectionValidator(connectionInfo);

            List<ValidationInfo> Results = connectionValidator.Validate();

            if (Results != null) 
            {
                ConsoleTable consoleTable = new ConsoleTable("EndPoint", "Type", "DNS Test", "Endpoint IP", "TCP Port (443)");

                foreach (ValidationInfo result in Results)
                { 
                    string Endpoint = result.Endpoint;

                    consoleTable.AddRow(Endpoint, result.Type.ToString(), result.DNSPass.ToString(), result.GetIPsAsString(), result.PingPass.ToString());
                }

                consoleTable.Print();
            }
        }

        public class ConnectionValidator
        {
            private ConnectionInfo connectionInfo;
            private List<ValidationInfo> Results;
            public ConnectionValidator(ConnectionInfo connectionInfo)
            {
                this.connectionInfo = connectionInfo;

                Results = new List<ValidationInfo>
                {
                    new ValidationInfo(connectionInfo.BlobEndpoint, StorageType.Blob),
                    new ValidationInfo(connectionInfo.FileEndpoint, StorageType.File),
                    new ValidationInfo(connectionInfo.QueueEndpoint, StorageType.Queue),
                    new ValidationInfo(connectionInfo.TableEndpoint, StorageType.Table)
                };
            }

            public List<ValidationInfo> Validate()
            {
                foreach (ValidationInfo info in Results)
                {
                    try
                    {
                        IPAddress[] IPs = Dns.GetHostAddresses(info.Endpoint);

                        info.IPs = IPs;
                        info.DNSPass = ValidateStatus.Succeeded;
                    }
                    catch
                    {
                        info.DNSPass = ValidateStatus.Failed;
                    }
                }

                foreach (ValidationInfo info in Results)
                {
                    if (!(info.DNSPass == ValidateStatus.Succeeded))
                    {
                        continue;
                    }

                    try
                    {
                        foreach (IPAddress ip in info.IPs)
                        {
                            using (TcpClient client = new TcpClient(ip.ToString(), 443)) { };
                        }

                        info.PingPass = ValidateStatus.Succeeded;
                        
                    }
                    catch
                    {
                        info.PingPass = ValidateStatus.Failed;
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

        public enum ValidateStatus
        { 
            Succeeded,
            Failed,
            NotApplicable
        }

        public class ValidationInfo
        {
            public string Endpoint;
            public StorageType Type;
            public IPAddress[] IPs;
            public ValidateStatus DNSPass;
            public ValidateStatus PingPass;

            public ValidationInfo(string Endpoint, StorageType Type)
            {
                this.Endpoint = Endpoint;
                this.Type = Type;
                DNSPass = ValidateStatus.NotApplicable;
                PingPass = ValidateStatus.NotApplicable;
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

                return String.Empty;
            }
        }

        public class ConnectionInfo
        {
            private Dictionary<string, string> CSInfo;
            public ConnectionInfo(string ConnectionString) 
            {
                CSInfo = new Dictionary<string, string>();

                string[] Infos = ConnectionString.Split(";");
                foreach (string Info in Infos)
                {
                    string[] KV = Info.Split("=");
                    CSInfo.Add(KV[0], KV[1]);
                }

                BlobEndpoint = $"{AccountName}.blob.{EndpointSuffix}";
                FileEndpoint = $"{AccountName}.file.{EndpointSuffix}";
                QueueEndpoint = $"{AccountName}.queue.{EndpointSuffix}";
                TableEndpoint = $"{AccountName}.table.{EndpointSuffix}";
            }

            public string BlobEndpoint { get; private set; }
            public string FileEndpoint { get; private set; }
            public string QueueEndpoint { get; private set; }
            public string TableEndpoint { get; private set; }

            public string DefaultEndpointsProtocol
            {
                get 
                {
                    return CSInfo["DefaultEndpointsProtocol"];
                }
            }

            public string AccountName
            {
                get
                {
                    return CSInfo["AccountName"];
                }
            }

            public string AccountKey
            {
                get
                {
                    return CSInfo["AccountKey"];
                }
            }

            public string EndpointSuffix
            {
                get
                {
                    return CSInfo["EndpointSuffix"];
                }
            }
        }
    }
}
