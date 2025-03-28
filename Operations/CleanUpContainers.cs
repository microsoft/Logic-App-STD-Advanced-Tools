using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LogicAppAdvancedTool.Operations
{
    public static class CleanUpContainers
    {
        public static void Run(string workflowName, string date)
        {
            int targetDate = Int32.Parse(date);
            string formattedDate = DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");

            List<string> containerPrefixs = new List<string>();
            if (string.IsNullOrEmpty(workflowName))
            {
                containerPrefixs.Add(StoragePrefixGenerator.GenerateLogicAppPrefix());
            }
            else
            {
                List<string> flowIDs = CommonOperations.ListFlowIDsByName(workflowName);
                foreach (string flowID in flowIDs)
                {
                    containerPrefixs.Add(StoragePrefixGenerator.GenerateWorkflowTablePrefixByFlowID(flowID));
                }
            }

            List<string> matchedContainers = new List<string>();
            BlobServiceClient client = StorageClientCreator.GenerateBlobServiceClient();

            foreach (string prefix in containerPrefixs)
            {
                string containerPrefix = $"flow{prefix}";
                List<BlobContainerItem> containers = client.GetBlobContainers(BlobContainerTraits.Metadata, BlobContainerStates.None, containerPrefix).ToList();

                List<string> containerList = containers
                                                .Where(x => int.Parse(x.Name.Substring(34, 8)) < targetDate)
                                                .Select(s => s.Name)
                                                .ToList();

                matchedContainers.AddRange(containerList);
            }

            if (matchedContainers.Count == 0)
            {
                throw new UserInputException($"No blob containers found.");
            }

            Console.WriteLine($"There are {matchedContainers.Count} containers found, please enter \"P\" to print the list or press any other key to continue without print list");
            if (Console.ReadLine().ToLower() == "p")
            {
                ConsoleTable table = new ConsoleTable(new List<string>() { "Contianer Name" });

                foreach (string containerName in matchedContainers)
                {
                    table.AddRow(new List<string> { containerName });
                }
                table.Print();
            }

            CommonOperations.PromptConfirmation($"Deleted those container will cause run history data lossing which executed before {formattedDate}");

            foreach (string containerName in matchedContainers)
            {
                client.DeleteBlobContainer(containerName);
            }

            Console.WriteLine("Clean up succeeded");
        }
    }
}
