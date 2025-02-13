using Azure.Data.Tables;
using LogicAppAdvancedTool.Shared;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using static System.Net.Mime.MediaTypeNames;

namespace LogicAppAdvancedTool.Operations
{
    public static class GenerateSAS
    {
        public static void Run(string workflowName)
        {
            string workflowID = WorkflowsInfoQuery.QueryWorkflowID(workflowName);
            string sv = "1.0";

            string definition = WorkflowsInfoQuery.QueryDefinitionByFlowName(workflowName);
            string triggerName = ((JObject)JObject.Parse(definition)["triggers"]).Properties().FirstOrDefault().Name;
            string permission = $"/triggers/{triggerName}/run";

            TableEntity accessKeyEntity = TableOperations.QueryAccessKeyTable($"FlowId eq '{workflowID}'", new string[] { "PrimaryKey", "SecondaryKey"}).FirstOrDefault();
            string primaryKey = accessKeyEntity.GetString("PrimaryKey");
            string secondaryKey = accessKeyEntity.GetString("SecondaryKey");

            string primarySignature = GenerateSignature(workflowID, sv, permission, primaryKey);
            string secondarySignature = GenerateSignature(workflowID, sv, permission, secondaryKey);

            string hostName = AppSettings.Hostname;

            Console.WriteLine($"Primary SAS: {primarySignature}");
            Console.WriteLine($"Secondary SAS: {secondarySignature}");
            string sp = $"{permission}";
            Console.WriteLine($"Primary callback url: https://{hostName}:443/api/{workflowName}/triggers/{triggerName}/invoke?api-version=2022-05-01&sp={HttpUtility.UrlEncode(sp)}&sv=1.0&sig={primarySignature}");
            Console.WriteLine($"Primary callback url: https://{hostName}:443/api/{workflowName}/triggers/{triggerName}/invoke?api-version=2022-05-01&sp={HttpUtility.UrlEncode(sp)}&sv=1.0&sig={secondarySignature}");
        }

        private static string GenerateSignature(string flowID, string sv, string permission, string accessKey)
        {
            string composedContent = $"{sv}.{flowID.ToUpperInvariant()}...{permission.ToUpperInvariant()}.{accessKey}";

            HashAlgorithm hash = SHA256.Create();
            byte[] hashBytes = hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(composedContent));
            string signature = Convert.ToBase64String(hashBytes);

            signature = signature.TrimEnd('=');
            signature = signature.Replace('+', '-');
            signature = signature.Replace('/', '_');

            return signature;
        }
    }
}
