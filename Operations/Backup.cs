using Azure;
using Azure.Data.Tables;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace LAVersionReverter
{
    partial class Program
    {
        private static void BackupDefinitions(string LogicAppName, string ConnectionString, uint Ago)
        {
            string DefinitionTableName = GetMainTableName(LogicAppName, ConnectionString);
            string BackupFolder = $"{Directory.GetCurrentDirectory()}/Backup";

            TableClient tableClient = new TableClient(ConnectionString, DefinitionTableName);
            Pageable<TableEntity> tableEntities;

            Directory.CreateDirectory(BackupFolder);

            Hashtable Appsettings = (Hashtable)Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process);
            File.WriteAllText($"{BackupFolder}/appsettings.json", JsonConvert.SerializeObject(Appsettings, Formatting.Indented));

            if (Ago != 0)
            {
                string TimeStamp = DateTime.Now.AddDays(-Ago).ToString("yyyy-MM-ddT00:00:00Z");
                tableEntities = tableClient.Query<TableEntity>(filter: $"ChangedTime ge datetime'{TimeStamp}'");
            }
            else
            { 
                tableEntities = tableClient.Query<TableEntity>();
            }

            foreach (TableEntity entity in tableEntities)
            {
                string RowKey = entity.GetString("RowKey");
                string FlowSequenceId = entity.GetString("FlowSequenceId");
                string FlowName = entity.GetString("FlowName");
                string ModifiedDate = ((DateTimeOffset)entity.GetDateTimeOffset("ChangedTime")).ToString("yyyy_MM_dd_HH_mm_ss");

                string BackupFlowPath = $"{BackupFolder}/{FlowName}";
                string BackupFilePath = $"{BackupFlowPath}/{ModifiedDate}_{FlowSequenceId}.json";

                //Filter for duplicate definition which in used
                if (!RowKey.Contains("FLOWVERSION"))
                {
                    continue;
                }

                //The definition has already been backup
                if (File.Exists(BackupFilePath))
                {
                    continue;
                }

                if (!Directory.Exists(BackupFlowPath))
                {
                    Directory.CreateDirectory(BackupFlowPath);
                }

                byte[] DefinitionCompressed = entity.GetBinary("DefinitionCompressed");
                string Kind = entity.GetString("Kind");
                string DecompressedDefinition = DecompressContent(DefinitionCompressed);

                string OutputContent = $"{{\"definition\": {DecompressedDefinition},\"kind\": \"{Kind}\"}}";

                File.WriteAllText(BackupFilePath, OutputContent);
            }
        }
    }
}
