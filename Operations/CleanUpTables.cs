using System;
using System.Collections.Generic;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using System.Globalization;
using Azure.Data.Tables;

namespace LogicAppAdvancedTool.Operations
{
    public static class CleanUpTables
    {
        public static void Run(string workflowName, string date)
        {
            int targetDate = Int32.Parse(date);
            string formattedDate = DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");

            List<string> tablePrefixs = new List<string>();
            if (string.IsNullOrEmpty(workflowName))
            {
                tablePrefixs.Add(StoragePrefixGenerator.GenerateLogicAppPrefix());
            }
            else
            {
                List<string> flowIDs = CommonOperations.ListFlowIDsByName(workflowName);
                foreach (string flowID in flowIDs)
                {
                    tablePrefixs.Add(StoragePrefixGenerator.GenerateWorkflowTablePrefixByFlowID(flowID));
                }
            }

            TableServiceClient client = StorageClientCreator.GenerateTableServiceClient();
            List<string> matchedTables = new List<string>();

            foreach (string prefix in tablePrefixs)
            {
                string tablePrefix = $"flow{prefix}";

                //List all the actions, variable table befire specific date
                List<string> tables = client.Query()
                                        .Where(x => x.Name.StartsWith(tablePrefix) && (x.Name.EndsWith("actions") || x.Name.EndsWith("variables")) && Int32.Parse(x.Name.Substring(34, 8)) < targetDate)
                                        .Select(s => s.Name)
                                        .ToList();

                matchedTables.AddRange(tables);
            }

            if (matchedTables.Count == 0)
            {
                throw new UserInputException($"No storage tables found.");
            }

            Console.WriteLine($"There are {matchedTables.Count} storage table found, please enter \"P\" to print the list or press any other key to continue without print");
            if (Console.ReadLine().ToLower() == "p")
            {
                ConsoleTable table = new ConsoleTable(new List<string>() { "Table Name" });

                foreach (string tableName in matchedTables)
                {
                    table.AddRow(new List<string>() { tableName });
                }

                table.Print();
            }

            CommonOperations.PromptConfirmation($"Deleted those storage tables will cause run history data lossing which executed before {formattedDate}");

            foreach (string tableName in matchedTables)
            {
                client.DeleteTable(tableName);
            }

            Console.WriteLine("Clean up succeeded");
        }
    }
}
