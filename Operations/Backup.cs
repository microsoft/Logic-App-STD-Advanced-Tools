using Azure.Data.Tables;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace LogicAppAdvancedTool.Operations
{
    public static class Backup
    {
        public static void Run(string date = "19700101")
        {
            //Create backup folder
            string backupFolder = $"{Directory.GetCurrentDirectory()}/Backup";
            Directory.CreateDirectory(backupFolder);

            string appSettings = AppSettings.GetRemoteAppsettings();
            File.WriteAllText($"{backupFolder}/appsettings.json", appSettings);

            string formattedDate = DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("yyyy-MM-ddT00:00:00Z");

            //In Storage Table, all the in-used workflows have duplicate records which start with "MYEDGEENVIRONMENT_FLOWIDENTIFIER" and "MYEDGEENVIRONMENT_FLOWLOOKUP"
            //Filter only for "MYEDGEENVIRONMENT_FLOWVERSION" to exclude duplicate workflow definitions
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
                string decompressedDefinition = CommonOperations.DecompressContent(definitionCompressed);

                string outputContent = $"{{\"definition\": {decompressedDefinition},\"kind\": \"{kind}\"}}";
                dynamic jsonObject = JsonConvert.DeserializeObject(outputContent);
                string formattedContent = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);

                File.WriteAllText(backupFilePath, formattedContent);
            }

            Console.WriteLine("Backup Succeeded.");
        }
    }
}
