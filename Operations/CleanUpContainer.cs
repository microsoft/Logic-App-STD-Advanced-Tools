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

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        public static void CleanUpContainer(string LogicAppName, string WorkflowName, string Date)
        {
            int TargetDate = Int32.Parse(Date);
            string FormattedDate = DateTime.ParseExact(Date, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");

            string Prefix;
            if (string.IsNullOrEmpty(WorkflowName))
            {
                Prefix = GenerateLogicAppPrefix(LogicAppName);
            }
            else
            {
                Prefix = GenerateWorkflowTablePrefix(LogicAppName, WorkflowName);
            }

            Prefix = $"flow{Prefix}";

            BlobServiceClient client = new BlobServiceClient(connectionString);
            List<BlobContainerItem> containers = client.GetBlobContainers(BlobContainerTraits.Metadata, BlobContainerStates.None, Prefix).ToList();

            if (containers.Count == 0)
            {
                Console.WriteLine($"No blob containers found for Logic App: {LogicAppName}");
            }

            List<string> ContainerList = new List<string>();

            foreach (BlobContainerItem item in containers)
            { 
                int CreatedDate = Int32.Parse(item.Name.Substring(34, 8));

                if (CreatedDate < TargetDate)
                {
                    ContainerList.Add(item.Name);   
                }
            }

            Console.WriteLine($"There are {ContainerList.Count} containers found, please enter \"P\" to print the list or press any other key to continue without print list");
            if (Console.ReadKey().Key.ToString().ToLower() == "p")
            {
                ConsoleTable table = new ConsoleTable("Contianer Name");

                foreach (string ContainerName in ContainerList)
                {
                    table.AddRow(ContainerName);
                }

                table.Print();
            }

            string ConfirmationMessage = $"WARNING!!!\r\nDeleted those container will cause run history data lossing which executed before {FormattedDate} \r\nPlease input for confirmation:";
            if (!Prompt.GetYesNo(ConfirmationMessage, false, ConsoleColor.Red))
            {
                Console.WriteLine("Operation Cancelled");

                return;
            }

            foreach (string ContainerName in ContainerList)
            { 
                client.DeleteBlobContainer(ContainerName);
            }

            Console.WriteLine("Clean up succeeded");
        }
    }
}
