using System;
using Azure.Storage.Queues;
using McMaster.Extensions.CommandLineUtils;

namespace LogicAppAdvancedTool
{
    public static class ClearJobQueue
    {
        public static void Run(string logicAppName)
        {
            CommonOperations.AlertExperimentalFeature();

            string queueName = $"flow{StoragePrefixGenerator.Generate(logicAppName.ToLower())}jobtriggers00";

            QueueClient queueClient = new QueueClient(AppSettings.ConnectionString, queueName);

            if (!queueClient.Exists())
            {
                throw new UserInputException($"Queue: {queueName} is not exist in Storage Account");
            }

            string confirmationMessage = "WARNING!!!\r\n1. Please make sure the Logic App has been stopped\r\n2. Clear Storage Queue will cause to lose data of all the running instances\r\nPlease input for confirmation:";
            if (!Prompt.GetYesNo(confirmationMessage, false, ConsoleColor.Red))
            {
                throw new UserCanceledException("Operation Cancelled");
            }

            queueClient.ClearMessages();
            Console.WriteLine($"Queue: {queueName} cleared, please restart Logic App");
        }
    }
}
