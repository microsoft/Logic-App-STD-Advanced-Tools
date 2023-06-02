using Azure;
using Azure.Data.Tables;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        private static void BackupDefinitions(string logicAppName, uint ago)
        {
            string definitionTableName = GetMainTableName(logicAppName);
            string backupFolder = $"{Directory.GetCurrentDirectory()}/Backup";

            TableClient tableClient = new TableClient(AppSettings.ConnectionString, definitionTableName);
            Pageable<TableEntity> tableEntities;

            Directory.CreateDirectory(backupFolder);

            Hashtable Appsettings = (Hashtable)Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process);
            File.WriteAllText($"{backupFolder}/appsettings.json", JsonConvert.SerializeObject(Appsettings, Formatting.Indented));

            if (ago != 0)
            {
                string timeStamp = DateTime.Now.AddDays(-ago).ToString("yyyy-MM-ddT00:00:00Z");
                tableEntities = tableClient.Query<TableEntity>(filter: $"ChangedTime ge datetime'{timeStamp}'");
            }
            else
            { 
                tableEntities = tableClient.Query<TableEntity>();
            }

            foreach (TableEntity entity in tableEntities)
            {
                string rowKey = entity.GetString("RowKey");
                string flowSequenceId = entity.GetString("FlowSequenceId");
                string flowName = entity.GetString("FlowName");
                string modifiedDate = ((DateTimeOffset)entity.GetDateTimeOffset("ChangedTime")).ToString("yyyy_MM_dd_HH_mm_ss");

                string backupFlowPath = $"{backupFolder}/{flowName}";
                string backupFilePath = $"{backupFlowPath}/{modifiedDate}_{flowSequenceId}.json";

                //Filter for duplicate definition which in used
                if (!rowKey.Contains("FLOWVERSION"))
                {
                    continue;
                }

                //The definition has already been backup
                if (File.Exists(backupFilePath))
                {
                    continue;
                }

                if (!Directory.Exists(backupFlowPath))
                {
                    Directory.CreateDirectory(backupFlowPath);
                }

                byte[] definitionCompressed = entity.GetBinary("DefinitionCompressed");
                string kind = entity.GetString("Kind");
                string decompressedDefinition = DecompressContent(definitionCompressed);

                string outputContent = $"{{\"definition\": {decompressedDefinition},\"kind\": \"{kind}\"}}";
                dynamic jsonObject = JsonConvert.DeserializeObject(outputContent);
                string formattedContent = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);

                File.WriteAllText(backupFilePath, formattedContent);

                Console.WriteLine("Backup Succeeded. You can download the definition.");
            }
        }
    }
}
