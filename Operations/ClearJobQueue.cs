using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using McMaster.Extensions.CommandLineUtils;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        public static void ClearJobQueue(string logicAppName, string connectionString)
        {
            string queueName = $"flow{StoragePrefixGenerator.Generate(logicAppName.ToLower())}jobtriggers00";

            QueueClient queueClient = new QueueClient(connectionString, queueName);

            if (!queueClient.Exists())
            {
                Console.WriteLine($"Queue: {queueName} is not exist in Storage Account");

                return;
            }

            string ConfirmationMessage = "CAUTION!!!\r\n1. Please make sure the Logic App has been stopped\r\n2. Clear Storage Queue will cause to lose data of all the running instances\r\nPlease input for confirmation:";
            if (!Prompt.GetYesNo(ConfirmationMessage, false))
            {
                Console.WriteLine("Operation Cancelled");

                return;
            }

            queueClient.ClearMessages();
            Console.WriteLine($"Queue: {queueName} cleared, please restart Logic App");
        }
    }
}
