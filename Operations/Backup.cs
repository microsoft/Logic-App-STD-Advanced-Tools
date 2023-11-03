using Azure.Data.Tables;
using Newtonsoft.Json;
using System;
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
            string backupFolder = $"{Directory.GetCurrentDirectory()}\\Backup";
            Directory.CreateDirectory(backupFolder);

            try
            {
                Console.WriteLine("Retrieving appsettings...");

                string appSettings = AppSettings.GetRemoteAppsettings();
                File.WriteAllText($"{backupFolder}\\appsettings.json", appSettings);

                Console.WriteLine("Backup for appsettings succeeded.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to retrieve appsettings, it will not be backup.\r\nException message: {ex.Message}");
            }

            string formattedDate = DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("yyyy-MM-ddT00:00:00Z");

            Console.WriteLine("Retrieving workflow definitions...");

            //In Storage Table, all the in-used workflows have duplicate records which start with "MYEDGEENVIRONMENT_FLOWIDENTIFIER" and "MYEDGEENVIRONMENT_FLOWLOOKUP"
            //Filter only for "MYEDGEENVIRONMENT_FLOWVERSION" to exclude duplicate workflow definitions
            List<TableEntity> tableEntities = TableOperations.QueryMainTable($"ChangedTime ge datetime'{formattedDate}'")
                                                            .Where(t => t.GetString("RowKey").StartsWith("MYEDGEENVIRONMENT_FLOWVERSION"))
                                                            .ToList();

            Console.WriteLine($"Found {tableEntities.Count} worklfow definiitons, saving to folder...");

            foreach (TableEntity entity in tableEntities)
            {
                string flowSequenceId = entity.GetString("FlowSequenceId");
                string flowName = entity.GetString("FlowName");
                string modifiedDate = ((DateTimeOffset)entity.GetDateTimeOffset("ChangedTime")).ToString("yyyy_MM_dd_HH_mm_ss");

                string backupFilePath = $"{backupFolder}\\{flowName}";
                string backupFileName = $"{modifiedDate}_{flowSequenceId}.json";

                //The definition has already been backup
                if (File.Exists($"{backupFilePath}\\{backupFileName}"))
                {
                    continue;
                }

                CommonOperations.SaveDefinition(backupFilePath, backupFileName, entity);
            }

            Console.WriteLine("Backup for workflow definitions succeeded.");
        }
    }
}
