using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.IO;
using LogicAppAdvancedTool.Structures;

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
            string sourceFolder = "C:\\home\\site\\wwwroot";

            CommonOperations.CopyDirectory(sourceFolder, backupPath, true);

            Console.WriteLine("Retrieving appsettings..");

            try
            {
                string appSettings = AppSettings.GetRemoteAppsettings();
                File.AppendAllText($"{backupPath}/appsettings.json", appSettings);

                Console.WriteLine("Appsettings backup successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to retrieve appsettings, only wwwroot folder will be backup.");
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

            string destinationFolder = "C:\\home\\site\\wwwroot";
            CommonOperations.CopyDirectory(path, destinationFolder, true);

            Console.WriteLine("All files are restored");

            Console.WriteLine("Restoring appsettings...");

            string appSettingPath = $"{path}/appsettings.json";

            if (!File.Exists(appSettingPath))
            {
                throw new ExpectedException("Warning!!! Missing appsettings.json, appsetting won't be restored.");
            }

            string appsettingsUrl = $"https://management.azure.com/subscriptions/{AppSettings.SubscriptionID}/resourceGroups/{AppSettings.ResourceGroup}/providers/Microsoft.Web/sites/{AppSettings.LogicAppName}/config/appsettings/list?api-version=2022-03-01";
            MSIToken token = MSITokenService.RetrieveToken("https://management.azure.com");
            string response = HttpOperations.HttpGetWithToken(appsettingsUrl, "POST", token.access_token, $"Cannot retrieve appsettings for {AppSettings.LogicAppName}");
            JToken appSettingRuntime = JObject.Parse(response);

            JToken appSettingsSnapshot = JObject.Parse(File.ReadAllText(appSettingPath));

            appSettingRuntime["properties"] = appSettingsSnapshot;

            string updateUrl = $"https://management.azure.com/subscriptions/{AppSettings.SubscriptionID}/resourceGroups/{AppSettings.ResourceGroup}/providers/Microsoft.Web/sites/{AppSettings.LogicAppName}/config/appsettings?api-version=2022-03-01";
            string updatedPayload = JsonConvert.SerializeObject(appSettingRuntime);
            HttpOperations.HttpSendWithToken(updateUrl, "PUT", updatedPayload, token.access_token, $"Failed to restore appsettings.");

            Console.WriteLine($"Restore successfully, Logic App will restart automatically to refresh appsettings.");
        }
        #endregion
    }
}
