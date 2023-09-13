using Azure.Data.Tables;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace LogicAppAdvancedTool.AzureService
{
    public class IPRule
    {
        public IPRule() { }

        public IPRule(string value)
        {
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            IPRule rule = obj as IPRule;
            if ((System.Object)rule == null)
            {
                return false;
            }

            return this.value == rule.value;
        }

        public string value { get; set; }

        [JsonProperty("ipMask")]
        private string ipMask { set { this.value = value; } }
    }

    public class RegisteredProvider
    {
        public string APIVersion { get; set; }
        public string UrlParameter { get; set; }
        public string MaskName { get; set; }
        public string RulePath { get; set; }

        public RegisteredProvider() { }
    }
}
