using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using LogicAppAdvancedTool.Structures;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace LogicAppAdvancedTool
{
    public class ServiceTagRetriever
    {
        private Dictionary<string, AzureServiceTagProperties> ServiceTags;

        public ServiceTagRetriever()
        {
            MSIToken token = MSITokenService.RetrieveToken("https://management.azure.com");
            string serviceTagUrl = $"https://management.azure.com/subscriptions/{AppSettings.SubscriptionID}/providers/Microsoft.Network/locations/{AppSettings.Region.ToLower()}/serviceTags?api-version=2023-09-01";
            string serviceTagResponse = HttpOperations.ValidatedHttpRequestWithToken(serviceTagUrl, HttpMethod.Get, null, token.access_token, "Cannot retrieve Azure Connector IP range from Internet.");

            JToken rawContent = JsonConvert.DeserializeObject<JToken>(serviceTagResponse)["values"];
            if (rawContent == null)
            {
                throw new ExpectedException("Cannot retrieve service tags due to permission issue, please assign Logic App MI with reader role on subscription level and retry command after 2 minutes.");
            }

            List<AzureServiceTag> tags = rawContent.ToObject<List<AzureServiceTag>>();

            ServiceTags = new Dictionary<string, AzureServiceTagProperties>();
            foreach (AzureServiceTag tag in tags)
            {
                ServiceTags.Add(tag.Name, tag.Properties);
            }
        }

        public List<string> GetIPs(string name, ServiceTagIPType type)
        {
            switch (type)
            { 
                case ServiceTagIPType.IPv4:
                    return ServiceTags[name].IPv4Prefixes;
                case ServiceTagIPType.IPv6:
                    return ServiceTags[name].IPv6Prefixes;
                default:
                    return ServiceTags[name].AddressPrefixes;
            }
        }
    }

    public class AzureServiceTag
    {
        [JsonProperty("name")]
        public string Name { get; private set; }

        [JsonProperty("id")]
        public string ID { get; private set; }

        [JsonProperty("serviceTagChangeNumber")]
        public string ServiceTagChangeNumber { get; private set; }

        [JsonProperty("properties")]
        public AzureServiceTagProperties Properties { get; private set; }

        public AzureServiceTag() { }
    }

    public class AzureServiceTagProperties
    {
        [JsonProperty("changeNumber")]
        public string ChangeNumber { get; private set; }

        [JsonProperty("region")]
        public string Region { get; private set; }

        [JsonProperty("state")]
        public string State { get; private set; }

        [JsonProperty("networkFeatures")]
        public List<string> NetworkFeatures { get; private set; }

        [JsonProperty("systemService")]
        public string SystemService { get; private set; }

        [JsonProperty("addressPrefixes")]
        public List<string> AddressPrefixes { get; private set; }

        public List<string> IPv4Prefixes { get; private set; }
        public List<string> IPv6Prefixes { get; private set; }

        public AzureServiceTagProperties() { }

        [JsonConstructor]
        public AzureServiceTagProperties(List<string> addressPrefixes)
        {
            AddressPrefixes = addressPrefixes;
            IPv4Prefixes = addressPrefixes.Where(p => p.Contains(".")).ToList();
            IPv6Prefixes = addressPrefixes.Where(p => p.Contains(":")).ToList();
        }
    }
}