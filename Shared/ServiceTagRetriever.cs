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
                tag.Properties.IPv4Prefixes = tag.Properties.AddressPrefixes.Where(p => p.Contains(".")).ToList();
                tag.Properties.IPv6Prefixes = tag.Properties.AddressPrefixes.Where(p => p.Contains(":")).ToList();

                ServiceTags.Add(tag.Name, tag.Properties);
            }
        }

        public List<string> GetIPsByName(string name)
        {
            return ServiceTags[name].AddressPrefixes;
        }

        public List<string> GetIPsV4ByName(string name)
        {
            return ServiceTags[name].IPv4Prefixes;
        }

        public List<string> GetIPsV6ByName(string name)
        {
            return ServiceTags[name].IPv6Prefixes;
        }
    }

    public class AzureServiceTag
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("serviceTagChangeNumber")]
        public string ServiceTagChangeNumber { get; set; }

        [JsonProperty("properties")]
        public AzureServiceTagProperties Properties { get; set; }

        public AzureServiceTag() { }
    }

    public class AzureServiceTagProperties
    {
        [JsonProperty("changeNumber")]
        public string ChangeNumber { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("networkFeatures")]
        public List<string> NetworkFeatures { get; set; }

        [JsonProperty("systemService")]
        public string SystemService { get; set; }

        [JsonProperty("addressPrefixes")]
        public List<string> AddressPrefixes { get; set; }

        public List<string> IPv4Prefixes { get; set; }
        public List<string> IPv6Prefixes { get; set; }

        public AzureServiceTagProperties() { }
    }
}