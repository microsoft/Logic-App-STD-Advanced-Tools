using System;
using System.IO;

namespace LogicAppAdvancedTool.Operations
{
    public static class Snapshot
    {
        #region Create a snapshot
        public static void CreateSnapshot()
        {
            string backupPath = $"Snapshot_{DateTime.Now.ToString("yyyyMMddHHmmss")}";

            if (Directory.Exists(backupPath))   //This should never be hit
            {
                throw new ExpectedException($"Folder with name {backupPath} already exist, snapshot will not be created, please try again.");
            }

            Directory.CreateDirectory(backupPath);

            Console.WriteLine("Backing up workflow related files (definition, artifacts, host.json, etc.)");
            CommonOperations.CopyDirectory(AppSettings.RootFolder, backupPath, true);
            Console.WriteLine("Backup for wwwroot folder succeeded.");

            Console.WriteLine("Retrieving appsettings..");

            try
            {
                string appSettings = AppSettings.GetRemoteAppsettings();
                File.AppendAllText($"{backupPath}/appsettings.json", appSettings);

                Console.WriteLine("Appsettings backup successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to retrieve appsettings, please review your Logic App Managed Identity role (Website Contributor or Logic App Standard Contributor required).");
            }

            Console.WriteLine($"Snapshot created, you can review all files in folder {backupPath}");
        }
        #endregion

        #region restore from Snapshot
        public static void RestoreSnapshot(string path)
        {
            if (!Directory.Exists(path))
            {
                throw new UserInputException($"Cannot find Snapshot path: {path}, please revew you input.");
            }

            Console.WriteLine("Restoring files in wwwroot folder.");

            CommonOperations.CopyDirectory(path, AppSettings.RootFolder, true);

            Console.WriteLine("All files are restored");

            Console.WriteLine("Restoring appsettings...");

            string appSettingPath = $"{path}/appsettings.json";

            if (!File.Exists(appSettingPath))
            {
                throw new ExpectedException("Warning!!! Missing appsettings.json, appsetting won't be restored.");
            }

            string appSettingsSnapshot = File.ReadAllText(appSettingPath);

            AppSettings.UpdateRemoteAppsettings(appSettingsSnapshot);

            Console.WriteLine($"Restore successfully, Logic App will restart automatically to refresh appsettings.");
        }
        #endregion
    }
}
