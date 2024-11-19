using Azure.Data.Tables;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace LogicAppAdvancedTool.Operations
{
    public static class Decode
    {
        public static void Run(string workflowName, string version)
        {
            TableEntity entity = TableOperations.QueryMainTable($"FlowName eq '{workflowName}' and FlowSequenceId eq '{version}'", new string[] { "DefinitionCompressed", "Kind", "RuntimeContext" }).FirstOrDefault();

            if (entity == null)
            {
                throw new UserInputException($"{workflowName} with version {version} cannot be found in storage table, please check your input.");
            }

            byte[] definitionCompressed = entity.GetBinary("DefinitionCompressed");
            string kind = entity.GetString("Kind");
            string decompressedDefinition = CommonOperations.DecompressContent(definitionCompressed);
            string definition = $"{{\"definition\": {decompressedDefinition},\"kind\": \"{kind}\"}}";

            dynamic jsonObject = JsonConvert.DeserializeObject(definition);
            string formattedContent = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);

            Console.Write(formattedContent);
        }
    }
}