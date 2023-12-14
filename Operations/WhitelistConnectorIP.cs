using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using System.Linq;
using LogicAppAdvancedTool.Structures;
using System.Net.Http;

namespace LogicAppAdvancedTool.Operations
{
    public static class WhitelistConnectorIP
    {
        public static void Run(string resourceID)
        {
            string[] resource = resourceID.TrimStart('/').Split('/');

            Dictionary<string, string> targetResourceInfo = new Dictionary<string, string>();
            for (int i = 0; i < resource.Length; i += 2)
            {
                targetResourceInfo.Add(resource[i], resource[i + 1]);
            }

            string providerContent = CommonOperations.GetEmbeddedResource("LogicAppAdvancedTool.Resources.RegisteredProvider.json");
            Dictionary<string, RegisteredProvider> supportedProviders = JsonConvert.DeserializeObject<Dictionary<string, RegisteredProvider>>(providerContent);
            string resourceProvider = targetResourceInfo["providers"];

            //validate whether the provided resource is supported or not
            if (!supportedProviders.ContainsKey(resourceProvider))
            {
                throw new UserInputException($"The provided resource provider: \"{resourceProvider}\" is not supported, following services are supported:\r\n{string.Join("\r\n", supportedProviders.Keys)}");
            }

            RegisteredProvider resourceProviderInfo = supportedProviders[resourceProvider];

            MSIToken token = MSITokenService.RetrieveToken("https://management.azure.com");

            string resourceUrl = $"https://management.azure.com{resourceID}{resourceProviderInfo.UrlParameter ?? String.Empty}?api-version={resourceProviderInfo.APIVersion}";
            string validateResponse = HttpOperations.ValidatedHttpRequestWithToken(resourceUrl, HttpMethod.Get, null, token.access_token, "Validate resource failed");

            JToken resourceProperties = JObject.Parse(validateResponse);

            //If resource firewall is not enabled yet, then need to add networkAcls field first to get ride of null reference exception
            if (resourceProperties.SelectToken(resourceProviderInfo.RulePath) == null)
            {
                if (resourceProviderInfo.RulePath == "properties")
                {
                    resourceProperties["properties"] = JToken.Parse("{\"ipRules\":[]}");
                }
                else
                {
                    resourceProperties["properties"]["networkAcls"] = JToken.Parse("{\"ipRules\":[]}");
                }
            }

            List<IPRule> resourceIPRules = JsonConvert.DeserializeObject<List<IPRule>>(resourceProperties.SelectToken(resourceProviderInfo.RulePath)["ipRules"].ToString());

            string subscriptionID = AppSettings.SubscriptionID;
            string region = AppSettings.Region.Replace(" ", "");

            Console.WriteLine($"Resource found in Azure, retrieving Azure Connector IP range in {region}");
            string serviceTagUrl = $"https://management.azure.com/subscriptions/{subscriptionID}/providers/Microsoft.Network/locations/{region.ToLower()}/serviceTags?api-version=2023-02-01";

            string serviceTagResponse = HttpOperations.ValidatedHttpRequestWithToken(serviceTagUrl, HttpMethod.Get, null, token.access_token, "Cannot grab Azure Connector IP range from Internet");

            JToken regionalIPInfo = JObject.Parse(serviceTagResponse)["values"].ToList()
                                    .Where(s => s["name"].ToString() == $"AzureConnectors.{region}")
                                    .FirstOrDefault();

            //storage account has a limitation which doesn't support /32 and /31.
            //In Azure IP Range json, connector service doesn't have any prefix with /31, just ignore
            List<string> IPs = regionalIPInfo["properties"]["addressPrefixes"].ToObject<List<string>>()
                                .Where(s => !s.Contains(":"))
                                .Select(s => s.Replace("/32", ""))      
                                .ToList();

            List<IPRule> validIPs = IPs.Where(s => !resourceIPRules
                                            .Select(p => new IPRule(p.value.Replace("/32", "")))
                                            .ToList()
                                            .Contains(new IPRule(s)))
                                        .Select(s => new IPRule(s))
                                        .ToList();

            if (validIPs.Count == 0)
            {
                Console.WriteLine($"Detected {IPs.Count} IP range from Azure document, all of them are in the firewall rule, no need to update.");

                return;
            }

            Console.WriteLine($"Detected {IPs.Count} IP range from Azure document, {validIPs.Count} records(s) not found in firewall, updating firewall records.");

            resourceIPRules.AddRange(validIPs);

            //switch netowrking seeting to "Enabled from selected virtual networks and IP addresses"
            resourceProperties.SelectToken(resourceProviderInfo.RulePath)["defaultAction"] = "Deny";
            resourceProperties["properties"]["publicNetworkAccess"] = "Enabled";

            string ipRulesStr = JsonConvert.SerializeObject(resourceIPRules, Formatting.Indented);

            //quick and dirty implementation
            if (!string.IsNullOrEmpty(resourceProviderInfo.MaskName))
            {
                ipRulesStr = ipRulesStr.Replace("value", resourceProviderInfo.MaskName);
            }

            resourceProperties.SelectToken(resourceProviderInfo.RulePath)["ipRules"] = JToken.Parse(ipRulesStr);
            string httpPayload = JsonConvert.SerializeObject(resourceProperties, Formatting.Indented);

            string ingestUrl = $"https://management.azure.com{resourceID}{resourceProviderInfo.UrlParameter ?? String.Empty}?api-version={resourceProviderInfo.APIVersion}";
            HttpOperations.ValidatedHttpRequestWithToken(ingestUrl, HttpMethod.Put, httpPayload, token.access_token, "Failed to add IP range");

            Console.WriteLine("Firewall updated, please refresh (press F5) the whole page.");
        }
    }
}
