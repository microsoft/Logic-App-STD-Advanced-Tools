using Azure.Data.Tables;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace LogicAppAdvancedTool.AzureService
{
    public class NetworkAcls
    {
        public NetworkAcls() { }

        public List<IPRule> ipRules { get; set; }
        public string defaultAction { get; set; }
        public string bypass { get; set; }
        public object virtualNetworkRules { get; set; }
    }

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
    }

    public class RegisteredProvider
    {
        public string APIVersion { get; set; }

        public RegisteredProvider() { }
    }
}
