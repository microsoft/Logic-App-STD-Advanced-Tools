using Azure.Data.Tables;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LogicAppAdvancedTool
{
    public static class Decode
    {
        public static void Run(string workflowName, string version)
        {
            List<TableEntity> tableEntities = TableOperations.QueryMainTable($"FlowName eq '{workflowName}' and FlowSequenceId eq '{version}'", new string[] { "DefinitionCompressed", "Kind" });

            if (tableEntities.Count() == 0)
            {
                throw new UserInputException($"{workflowName} with version {version} cannot be found in storage table, please check your input.");
            }

            TableEntity entity = tableEntities.FirstOrDefault();

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