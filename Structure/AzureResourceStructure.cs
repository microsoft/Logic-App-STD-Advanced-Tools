using Azure.Data.Tables;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace LogicAppAdvancedTool.AzureService
{
    #region base class
    public class AzureResourceBase
    {
        public AzureResourceBase() { }

        public string location { get; set; }
        public ResourcePropertiesBase properties { get; set; }
    }
    public class ResourcePropertiesBase
    {
        public object sku { get; set; }
        public string tenantId { get; set; }
        public NetworkAclsBase networkAcls { get; set; }
    }

    public class NetworkAclsBase
    {
        public NetworkAclsBase() { }

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
    #endregion

    #region Key Vault related
    public class KeyVaultResource : AzureResourceBase
    {
        public KeyVaultResource() { }

        public new KeyVaultProperties properties { get; set; }
    }

    public class KeyVaultProperties : ResourcePropertiesBase
    {
        public KeyVaultProperties() { }
        public object accessPolicies { get; set; }
    }
    #endregion

    #region Storage related
    public class StorageResource : AzureResourceBase
    {
        public StorageResource() { }

        public new StorageProperties properties { get; set; }
    }

    public class StorageProperties : ResourcePropertiesBase
    {
        public StorageProperties() { }

        public new StorageNetworkAcls networkAcls { get; set; }
        public string publicNetworkAccess;
    }

    public class StorageNetworkAcls : NetworkAclsBase
    {
        public StorageNetworkAcls() { }

        public object resourceAccessRules { get; set; }
    }
    #endregion

    public class RegisteredProvider
    {
        public string APIVersion { get; set; }

        public RegisteredProvider() { }
    }
}
