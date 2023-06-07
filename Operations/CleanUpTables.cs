using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using McMaster.Extensions.CommandLineUtils;
using Azure.Storage.Blobs;
using Azure;
using Azure.Storage.Blobs.Models;
using System.Globalization;
using Azure.Data.Tables;
using System.Reflection.Metadata.Ecma335;
using Azure.Data.Tables.Models;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        public static void CleanUpTables(string logicAppName, string workflowName, string date)
        {
            int targetDate = Int32.Parse(date);
            string formattedDate = DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");

            string tablePrefix;
            if (string.IsNullOrEmpty(workflowName))
            {
                tablePrefix = GenerateLogicAppPrefix(logicAppName);
            }
            else
            {
                tablePrefix = GenerateWorkflowTablePrefix(logicAppName, workflowName);
            }

            tablePrefix = $"flow{tablePrefix}";

            TableServiceClient client = new TableServiceClient(AppSettings.ConnectionString);
            List<string> tables = client.Query()
                                    .Where(x => 
                                        x.Name.StartsWith(tablePrefix) && (x.Name.EndsWith("actions") || x.Name.EndsWith("variables")) && Int32.Parse(x.Name.Substring(34,8)) < targetDate)
                                    .Select(s => s.Name)
                                    .ToList();

            if (tables.Count == 0)
            {
                Console.WriteLine($"No storage tables found.");
            }

            Console.WriteLine($"There are {tables.Count} storage table found, please enter \"P\" to print the list or press any other key to continue without print list");
            if (Console.ReadLine().ToLower() == "p")
            {
                ConsoleTable table = new ConsoleTable("Table Name");

                foreach (string tableName in tables)
                {
                    table.AddRow(tableName);
                }

                table.Print();
            }

            string confirmationMessage = $"WARNING!!!\r\nDeleted those storage tables will cause run history data lossing which executed before {formattedDate} \r\nPlease input for confirmation:";
            if (!Prompt.GetYesNo(confirmationMessage, false, ConsoleColor.Red))
            {
                Console.WriteLine("Operation Cancelled");

                return;
            }

            foreach (string tableName in tables) 
            { 
                client.DeleteTable(tableName);
            }

            Console.WriteLine("Clean up succeeded");
        }
    }
}
