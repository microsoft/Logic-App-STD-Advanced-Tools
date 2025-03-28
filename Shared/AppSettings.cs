using Azure.Core;
using LogicAppAdvancedTool.Structures;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;

namespace LogicAppAdvancedTool
{
    public class AppSettings
    {
        public static string ConnectionString
        {
            get
            {
                return Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            }
        }

        public static string TableServiceUri
        {
            get
            {
                return Environment.GetEnvironmentVariable("AzureWebJobsStorage__tableServiceUri");
            }
        }

        public static string BlobServiceUri
        {
            get
            {
                return Environment.GetEnvironmentVariable("AzureWebJobsStorage__blobServiceUri");
            }
        }

        public static string QueueServiceUri
        {
            get
            {
                return Environment.GetEnvironmentVariable("AzureWebJobsStorage__queueServiceUri");
            }
        }

        public static string ManagedIdentityResourceID
        {
            get
            {
                return Environment.GetEnvironmentVariable("AzureWebJobsStorage__managedIdentityResourceId");
            }
        }

        public static string HostID
        {
            get
            {
                return Environment.GetEnvironmentVariable("AzureFunctionsWebHost__hostId") ?? LogicAppName;
            }
        }

        public static string FileShareConnectionString
        {
            get
            {
                return Environment.GetEnvironmentVariable("WEBSITE_CONTENTAZUREFILECONNECTIONSTRING");
            }
        }

        public static string SubscriptionID
        {
            get 
            {
                return Environment.GetEnvironmentVariable("WEBSITE_OWNER_NAME").Split('+')[0];
            }
        }

        public static string ResourceGroup
        {
            get
            { 
                return Environment.GetEnvironmentVariable("WEBSITE_RESOURCE_GROUP");
            }
        }

        public static string Region
        {
            get
            {
                return Environment.GetEnvironmentVariable("REGION_NAME");
            }
        }

        public static string LogicAppName
        {
            get
            {
                return Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");
            }
        }

        public static string MSIEndpoint
        {
            get
            {
                return Environment.GetEnvironmentVariable("MSI_ENDPOINT");
            }
        }

        public static string MSISecret
        {
            get
            {
                return Environment.GetEnvironmentVariable("MSI_SECRET");
            }
        }

        public static string Hostname
        {
            get
            {
                return Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME");
            }
        }

        public static string RootFolder
        {
            get
            {
                return "C:\\home\\site\\wwwroot";
            }
        }

        public static string ManagementBaseUrl
        {
            get
            {
                return $"https://management.azure.com/subscriptions/{SubscriptionID}/resourceGroups/{ResourceGroup}/providers/Microsoft.Web/sites/{LogicAppName}";
            }
        }

        public static string GetValueByName(string name)
        { 
            return Environment.GetEnvironmentVariable(name);
        }

        public static string GetRemoteAppsettings()
        {
            string Url = $"{ManagementBaseUrl}/config/appsettings/list?api-version=2022-03-01";

            MSIToken token = MSITokenService.RetrieveToken("https://management.azure.com");
            string response = HttpOperations.ValidatedHttpRequestWithToken(Url, HttpMethod.Post, null, token.access_token, $"Cannot retrieve appsettings for {LogicAppName}");

            string appSettings = JsonConvert.SerializeObject(JObject.Parse(response)["properties"], Formatting.Indented);

            return appSettings;
        }

        public static void UpdateRemoteAppsettings(string appsettingContent)
        {
            string appsettingsUrl = $"{ManagementBaseUrl}/config/appsettings/list?api-version=2022-03-01";
            MSIToken token = MSITokenService.RetrieveToken("https://management.azure.com");
            string response = HttpOperations.ValidatedHttpRequestWithToken(appsettingsUrl, HttpMethod.Post, null, token.access_token, $"Cannot retrieve appsettings for {LogicAppName}");
            JToken appSettingRuntime = JObject.Parse(response);

            appSettingRuntime["properties"] = JObject.Parse(appsettingContent);

            string updateUrl = $"{ManagementBaseUrl}/config/appsettings?api-version=2022-03-01";
            string updatedPayload = JsonConvert.SerializeObject(appSettingRuntime);
            HttpOperations.ValidatedHttpRequestWithToken(updateUrl, HttpMethod.Put, updatedPayload, token.access_token, $"Failed to restore appsettings.");
        }
    }
}
