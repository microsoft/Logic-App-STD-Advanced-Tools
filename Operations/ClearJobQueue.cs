using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Queues;

namespace LAVersionReverter
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

            queueClient.ClearMessages();
            Console.WriteLine($"Queue: {queueName} cleared");
        }
    }
}
