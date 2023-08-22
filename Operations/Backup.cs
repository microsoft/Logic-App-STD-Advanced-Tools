using Azure.Data.Tables;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        private static void BackupDefinitions(string date = "19700101")
        {

            string backupFolder = $"{Directory.GetCurrentDirectory()}/Backup";
            Directory.CreateDirectory(backupFolder);

            Hashtable Appsettings = (Hashtable)Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process);
            File.WriteAllText($"{backupFolder}/appsettings.json", JsonConvert.SerializeObject(Appsettings, Formatting.Indented));

            string formattedDate = DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("yyyy-MM-ddT00:00:00Z");

            List<TableEntity> tableEntities = TableOperations.QueryMainTable($"ChangedTime ge datetime'{formattedDate}'")
                                                            .Where(t => t.GetString("RowKey").StartsWith("MYEDGEENVIRONMENT_FLOWVERSION"))
                                                            .ToList();

            foreach (TableEntity entity in tableEntities)
            {
                string rowKey = entity.GetString("RowKey");
                string flowSequenceId = entity.GetString("FlowSequenceId");
                string flowName = entity.GetString("FlowName");
                string modifiedDate = ((DateTimeOffset)entity.GetDateTimeOffset("ChangedTime")).ToString("yyyy_MM_dd_HH_mm_ss");

                string backupFlowPath = $"{backupFolder}/{flowName}";
                string backupFilePath = $"{backupFlowPath}/{modifiedDate}_{flowSequenceId}.json";

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
            }

            Console.WriteLine("Backup Succeeded.");
        }
    }
}
