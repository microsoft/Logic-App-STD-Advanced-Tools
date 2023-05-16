using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Microsoft.VisualBasic;
using Microsoft.WindowsAzure.ResourceStack.Common.Utilities;
using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;

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
    }
}
