using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicAppAdvancedTool
{
    public enum PrivateEndpointStatus
    { 
        Yes,
        No,
        Skipped
    }

    public enum ValidationStatus
    {
        Succeeded,
        Failed,
        NotApplicable,
        Skipped,
        EmptyEndpoint
    }

    public enum StorageServiceType
    {
        Blob = 1,
        File = 2,
        Queue = 4,
        Table = 8
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
        sql,
        NotSupported
    }

    public enum ServiceTagIPType
    { 
        IPv4,
        IPv6,
        All
    }
}
