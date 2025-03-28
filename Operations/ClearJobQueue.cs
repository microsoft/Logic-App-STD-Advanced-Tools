using System;
using Azure.Storage.Queues;
using McMaster.Extensions.CommandLineUtils;

namespace LogicAppAdvancedTool.Operations
{
    public static class ClearJobQueue
    {
        public static void Run(string logicAppName)
        {
            CommonOperations.AlertExperimentalFeature();

            string queueName = $"flow{StoragePrefixGenerator.GenerateLogicAppPrefix()}jobtriggers00";

            QueueClient queueClient = StorageClientCreator.GenerateQueueServiceClient().GetQueueClient(queueName);

            if (!queueClient.Exists())
            {
                throw new UserInputException($"Queue: {queueName} is not exist in Storage Account");
            }

            CommonOperations.PromptConfirmation("1. Please make sure the Logic App has been stopped\r\n2. Clear Storage Queue will cause to lose data of all the running instances");

            queueClient.ClearMessages();
            Console.WriteLine($"Queue: {queueName} cleared, please restart Logic App");
        }
    }
}
