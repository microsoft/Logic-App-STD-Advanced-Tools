using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using Microsoft.WindowsAzure.ResourceStack.Common.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        private static void ValidateServiceProviderConnectivity()
        {
            string connectionPath = "C:/home/site/wwwroot/connections.json";

            if (!File.Exists(connectionPath))
            {
                throw new ExpectedException($"connections.json not found in path: {connectionPath}");
            }

            Console.WriteLine("connections.json found, reading Service Provider information.");

            string content = File.ReadAllText(connectionPath);

            Dictionary<string, JToken> connections = JObject.Parse(content)["serviceProviderConnections"].ToObject<Dictionary<string, JToken>>();

            if (connections.Keys.Count == 0)
            {
                throw new ExpectedException("No Service Provider found in connections.json");
            }

            Console.WriteLine($"Found {connections.Keys.Count} Service Provider(s) in connections.json.");

            List<ServiceProviderValidator> spInfos = new List<ServiceProviderValidator>();
            List<ServiceProviderValidator> emptyEndpoints = new List<ServiceProviderValidator>();
            List<ServiceProviderValidator> unsupportedEndpoints = new List<ServiceProviderValidator>();

            foreach (string key in connections.Keys)
            {
                ServiceProviderValidator spv = GenerateServiceProviderInfo(key, connections[key]);

                if (spv.ServiceProvider == ServiceProviderType.NotSupported)
                {
                    unsupportedEndpoints.Add(spv);

                    continue;
                }

                if (spv.IsEndpointEmpty)
                {
                    emptyEndpoints.Add(spv);

                    continue;
                }

                spInfos.Add(spv);
            }

            if (emptyEndpoints.Count != 0)
            {
                Console.WriteLine($"Cannot find Endpoint for following Service Provider(s), please verify appsettings or connections.json.");

                ConsoleTable emptySPs = new ConsoleTable("Reference Name", "Display Name");

                foreach (ServiceProviderValidator spv in emptyEndpoints)
                {
                    emptySPs.AddRow(spv.Name, spv.DisplayName);
                }

                emptySPs.Print();
            }

            if (unsupportedEndpoints.Count != 0)
            {
                Console.WriteLine($"Following service provider(s) not supported yet.");

                ConsoleTable unsupportedSPs = new ConsoleTable("Reference Name", "Display Name");

                foreach (ServiceProviderValidator spv in unsupportedEndpoints)
                {
                    unsupportedSPs.AddRow(spv.Name, spv.DisplayName);
                }

                unsupportedSPs.Print();
            }

            Console.WriteLine($"Found {spInfos.Count} validate Service Provider(s), testing DNS resolution and tcp connection.");

            foreach (ServiceProviderValidator spv in spInfos)
            { 
                spv.Validate();
            }

            ConsoleTable consoleTable = new ConsoleTable("Name", "Display Name",  "DNS Status", "IP", "Port", "Connection Status");

            foreach (ServiceProviderValidator spv in spInfos)
            {
                string ip = spv.IP == null ? "N/A" : spv.IP.ToString();

                consoleTable.AddRow(spv.Name, spv.DisplayName, spv.DNSTestResult, ip, spv.Port.ToString(), spv.ConnectionTestResult);
            }

            consoleTable.Print();
        }

        private static ServiceProviderValidator GenerateServiceProviderInfo(string name, JToken content)
        {
            string providerType = content.GetProperty("serviceProvider").GetProperty("id").ToString().Split('/')[2];

            string authProvider = content.GetProperty("parameterSetName")?.ToString();
            ServiceAuthProvider serviceAuthProvider = String.IsNullOrEmpty(authProvider) ? ServiceAuthProvider.None : (ServiceAuthProvider)Enum.Parse(typeof(ServiceAuthProvider), authProvider);

            string displayName = content.GetProperty("displayName").ToString();
            JToken parameterValues = content.GetProperty("parameterValues");

            ServiceProviderType serviceProviderType = ServiceProviderType.NotSupported;

            try
            {
                serviceProviderType = (ServiceProviderType)Enum.Parse(typeof(ServiceProviderType), providerType);
            }
            catch (Exception ex)
            {
                return new ServiceProviderValidator(name, displayName, serviceProviderType, string.Empty, 0);
            }

            string connEndpoint = String.Empty;
            string connPort = String.Empty;

            switch (serviceProviderType)
            {
                case ServiceProviderType.DB2:
                    connEndpoint = parameterValues.GetProperty("serverName").ToString();
                    connPort = parameterValues.GetProperty("portNumber").ToString();
                    break;
                case ServiceProviderType.Ftp:
                    connEndpoint = parameterValues.GetProperty("serverAddress").ToString();
                    connPort = parameterValues.GetProperty("portNumber")?.ToString() ?? "21";
                    break;
                case ServiceProviderType.Sftp:
                    connEndpoint = parameterValues.GetProperty("sshHostAddress").ToString();
                    connPort = parameterValues.GetProperty("portNumber")?.ToString() ?? "22";
                    break;
                case ServiceProviderType.Smtp:
                    connEndpoint = parameterValues.GetProperty("serverAddress").ToString();
                    connPort = parameterValues.GetProperty("port")?.ToString() ?? "587";
                    break;
                case ServiceProviderType.eventGridPublisher:
                    connEndpoint = parameterValues.GetProperty("topicEndpoint").ToString();
                    connPort = "443";
                    break;
                case ServiceProviderType.keyVault:
                    connEndpoint = parameterValues.GetProperty("VaultUri").ToString();
                    connPort = "443";
                    break;
                case ServiceProviderType.mq:
                    connEndpoint = parameterValues.GetProperty("serverName").ToString();
                    connPort = parameterValues.GetProperty("portNumber").ToString();
                    break;
                default:
                    connEndpoint = DecodeEndpoint(parameterValues);
                    connPort = connEndpoint;    //except for above service provider, the port is either a static value or inside connection string, provide connection string for now
                    break;
            }

            string endpoint = FormatEndpoint(connEndpoint, serviceProviderType, serviceAuthProvider);
            endpoint = ConvertToBaseUri(endpoint);

            connPort = FormatPort(connPort, serviceProviderType, serviceAuthProvider);
            int port = String.IsNullOrEmpty(connPort) ? 0 : Int32.Parse(connPort);

            return new ServiceProviderValidator(name, displayName, serviceProviderType, endpoint, port);
        }

        #region retrieve Endpoint from connections.json
        private static string DecodeEndpoint(JToken parameterValues)
        {
            List<JProperty> props = parameterValues.CoalesceProperties().ToList();
            foreach (JProperty prop in props)
            {
                string[] prefix = new string[] { "Endpoint", "connectionString", "fullyQualifiedNamespace" };

                List<string> alternativeResults = prefix.Where(x => prop.Name.Contains(x))
                                                        .Select(y => prop.Value.ToString())
                                                        .ToList();

                if (alternativeResults.Count != 0)
                {
                    return alternativeResults[0];   //Assume only one pattern could match in parameter value
                }
            }

            return String.Empty;
        }

        private static string FormatEndpoint(string endpoint, ServiceProviderType serviceProviderType, ServiceAuthProvider serviceAuthProvider)
        {
            string formattedEndpoint = endpoint;

            //some endpoints refer to appsettings, need to pick up it from appsettings
            if (endpoint.StartsWith("@appsetting"))
            {
                string appsettingName = endpoint.Replace("@appsetting('", string.Empty).Replace("')", string.Empty);

                formattedEndpoint = Environment.GetEnvironmentVariable(appsettingName);
            }

            if (String.IsNullOrEmpty(formattedEndpoint))
            {
                return String.Empty;
            }

            switch (serviceProviderType)
            {
                case ServiceProviderType.AzureBlob:
                    formattedEndpoint = FormatStorageEndpoint("blob", formattedEndpoint, serviceAuthProvider);
                    break;
                case ServiceProviderType.AzureFile:
                    formattedEndpoint = FormatStorageEndpoint("file", formattedEndpoint, serviceAuthProvider);
                    break;
                case ServiceProviderType.azurequeues:
                    formattedEndpoint = FormatStorageEndpoint("queue", formattedEndpoint, serviceAuthProvider);
                    break;
                case ServiceProviderType.azureTables:
                    formattedEndpoint = FormatStorageEndpoint("table", formattedEndpoint, serviceAuthProvider);
                    break;
                case ServiceProviderType.AzureCosmosDB:
                    formattedEndpoint = FormatCosmosEndpoint(formattedEndpoint);
                    break;
                case ServiceProviderType.eventHub:
                    formattedEndpoint = FormatEventHubEndpoint(formattedEndpoint, serviceAuthProvider);
                    break;
                case ServiceProviderType.serviceBus:
                    formattedEndpoint = FormatServiceBusEndpoint(formattedEndpoint, serviceAuthProvider);
                    break;
                case ServiceProviderType.sql:
                    formattedEndpoint = FormatSQLEndpoint(formattedEndpoint, serviceAuthProvider);
                    break;
                default:
                    break;
            }

            return formattedEndpoint;
        }

        #region Format endpoint based on different service provider
        private static string FormatStorageEndpoint(string service, string endpoint, ServiceAuthProvider serviceAuthProvider)
        {
            switch (serviceAuthProvider)
            {
                case ServiceAuthProvider.connectionString:
                case ServiceAuthProvider.None:  //For storage file 
                    Dictionary<string, string> csInfo = TruncateConnectionString(endpoint);
                    return $"{csInfo["AccountName"]}.{service}.{csInfo["EndpointSuffix"]}";
            }

            return endpoint;
        }

        private static string FormatCosmosEndpoint(string endpoint)
        {
            Dictionary<string, string> csInfo = TruncateConnectionString(endpoint);
            return $"{csInfo["AccountEndpoint"]}";
        }

        private static string FormatEventHubEndpoint(string endpoint, ServiceAuthProvider serviceAuthProvider)
        {
            switch (serviceAuthProvider)
            {
                case ServiceAuthProvider.connectionString:
                    Dictionary<string, string> csInfo = TruncateConnectionString(endpoint);
                    return $"{csInfo["Endpoint"]}";
            }

            return endpoint;
        }

        private static string FormatServiceBusEndpoint(string endpoint, ServiceAuthProvider serviceAuthProvider)
        {
            switch (serviceAuthProvider)
            {
                case ServiceAuthProvider.connectionString:
                    Dictionary<string, string> csInfo = TruncateConnectionString(endpoint);
                    return $"{csInfo["Endpoint"]}";
            }

            return endpoint;
        }

        private static string FormatSQLEndpoint(string endpoint, ServiceAuthProvider serviceAuthProvider)
        {
            switch (serviceAuthProvider)
            {
                case ServiceAuthProvider.connectionString:
                    Dictionary<string, string> csInfo = TruncateConnectionString(endpoint);
                    return $"{csInfo["Server"]}";
            }

            return endpoint;
        }

        private static Dictionary<string, string> TruncateConnectionString(string connectionString)
        {
            Dictionary<string, string> csInfo = new Dictionary<string, string>();

            string[] infos = connectionString.Split(";");
            foreach (string info in infos)
            {
                if (String.IsNullOrEmpty(info))
                {
                    continue;
                }

                int index = info.IndexOf('=');
                string key = info.Substring(0, index);
                string value = info.Substring(index + 1, info.Length - index - 1);

                csInfo.Add(key, value);
            }

            return csInfo;
        }
        #endregion

        private static string ConvertToBaseUri(string url)
        {
            string formattedUrl = url.Replace("https://", String.Empty).Replace("sb://", String.Empty).Replace("tcp:", String.Empty);
            formattedUrl = formattedUrl.Split('/')[0];
            formattedUrl = formattedUrl.Split(':')[0];
            formattedUrl = formattedUrl.Split(',')[0];

            return formattedUrl;
        }
        #endregion

        #region retrieve Port from connections.json
        private static string FormatPort(string port, ServiceProviderType serviceProviderType, ServiceAuthProvider serviceAuthProvider)
        {
            string formattedPort = port;

            //some endpoints refer to appsettings, need to pick up it from appsettings
            if (port.StartsWith("@appsetting"))
            {
                string appsettingName = port.Replace("@appsetting('", string.Empty).Replace("')", string.Empty);

                formattedPort = Environment.GetEnvironmentVariable(appsettingName);
            }

            if (String.IsNullOrEmpty(formattedPort))
            {
                return String.Empty;
            }

            switch (serviceProviderType)
            {
                case ServiceProviderType.AzureBlob:
                case ServiceProviderType.AzureFile:
                case ServiceProviderType.azurequeues:
                case ServiceProviderType.azureTables:
                    formattedPort = "443";
                    break;
                case ServiceProviderType.serviceBus:
                    formattedPort = "5671";
                    break;
                case ServiceProviderType.AzureCosmosDB:
                    formattedPort = "443";
                    break;
                case ServiceProviderType.eventHub:
                    formattedPort = "443";
                    break;
                case ServiceProviderType.sql:
                    formattedPort = FormatSQLEndpoint(formattedPort, serviceAuthProvider).Split(',')[1];
                    break;
                default:
                    break;
            }

            return formattedPort;
        }
        #endregion
    }
}
