using Azure.Data.Tables;
using LogicAppAdvancedTool.Structures;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace LogicAppAdvancedTool
{
    internal partial class Tools
    {
        public static void GeneratePrefix(string logicAppName, string workflowID)
        {
            string logicAppPrefix = StoragePrefixGenerator.Generate(AppSettings.LogicAppName.ToLower());

            //if we don't need to generate workflow prefix, just output Logic App prefix
            if (String.IsNullOrEmpty(workflowID))
            {
                Console.WriteLine($"Logic App Prefix: {logicAppPrefix}");

                return;
            }

            string workflowPrefix = StoragePrefixGenerator.Generate(workflowID);

            Console.WriteLine($"Logic App Prefix: {logicAppPrefix}");
            Console.WriteLine($"Workflow Prefix: {workflowPrefix}");
            Console.WriteLine($"Combined prefix: {logicAppPrefix}{workflowPrefix}");
        }
    }
}
