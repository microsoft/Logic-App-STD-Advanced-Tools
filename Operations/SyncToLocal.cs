using Azure.Storage.Files.Shares.Models;
using Azure.Storage.Files.Shares;
using McMaster.Extensions.CommandLineUtils;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using Azure;

namespace LogicAppAdvancedTool.Operations
{
    public static class CloudSync
    {
        #region Normal Sync
        public static void SyncToLocal(string shareName, string connectionString, string localPath)
        {
            ShareClient shareClient = new ShareClient(connectionString, shareName);

            ShareDirectoryClient directoryClient = shareClient.GetDirectoryClient("site/wwwroot/");

            CommonOperations.PromptConfirmation("This operation will overwrite your local project files.");

            string syncModeMessage = "Whether clean up workflows in local project which cannot be found on cloud?\r\n\tYes: Clean up all the subfolders which not in clould (except .git, .vscode).\r\n\tNo: Only overwrite the files which modified on cloud, no files will be deleted.\r\nPlease input for confirmation:";
            if (Prompt.GetYesNo(syncModeMessage, false, ConsoleColor.Green))
            {
                List<string> excludeFolders = new List<string>() { ".git", ".vscode" };

                string excludeFoldersMessage = "Please provide the folders which you would like to exclude for clean up, use comma for split.\r\nIf no extra folder need to be excluded, just press Enter. (.git, .vscode folder will be excluded by default)";

                string customizedExcludes = Prompt.GetString(excludeFoldersMessage, null, ConsoleColor.Green);
                if (!string.IsNullOrEmpty(customizedExcludes))
                {
                    string[] exclude = customizedExcludes.Split(',');
                    foreach (string excludeItem in exclude)
                    {
                        excludeFolders.Add(excludeItem.Trim());
                    }
                }

                DirectoryInfo directoryInfo = new DirectoryInfo(localPath);
                DirectoryInfo[] subFolders = directoryInfo.GetDirectories();

                foreach (DirectoryInfo subFolder in subFolders)
                {
                    if (!excludeFolders.Contains(subFolder.Name))
                    {
                        Directory.Delete(subFolder.FullName, true);
                    }
                }
            }

            Sync(localPath, directoryClient);

            Console.WriteLine($"Sync to local successed, File Share name {shareName}.");
        }
        #endregion

        public static void BatchSyncToLocal(string configFile)
        {
            if (!File.Exists(configFile))
            {
                throw new UserInputException($"{configFile} cannot be found, please check your input");
            }

            string configContent = File.ReadAllText(configFile);
            List<SyncConfig> syncConfigs = JsonConvert.DeserializeObject<List<SyncConfig>>(configContent);

            foreach (SyncConfig config in syncConfigs)
            {
                AutoSyncToLocal(config.FileShareName, config.ConnectionString, config.LocalPath, config.Excludes);
            }

            Console.WriteLine("All the projects have been synced");
        }

        #region Auto sync
        public static void AutoSyncToLocal(string shareName, string connectionString, string localPath, List<string> excludes)
        {
            ShareClient shareClient = new ShareClient(connectionString, shareName);
            ShareDirectoryClient directoryClient = shareClient.GetDirectoryClient("site/wwwroot/");

            List<string> excludeFolders = new List<string>() { ".git", ".vscode" };

            if (excludes != null)
            {
                foreach (string excludeItem in excludes)
                {
                    excludeFolders.Add(excludeItem.Trim());
                }
            }

            DirectoryInfo directoryInfo = new DirectoryInfo(localPath);
            DirectoryInfo[] subFolders = directoryInfo.GetDirectories();

            foreach (DirectoryInfo subFolder in subFolders)
            {
                if (!excludeFolders.Contains(subFolder.Name))
                {
                    Directory.Delete(subFolder.FullName, true);
                }
            }

            Sync(localPath, directoryClient);

            Console.WriteLine($"Sync to local successed, File Share name {shareName}.");
        }
        #endregion

        private static void Sync(string localFolder, ShareDirectoryClient client)
        {
            Pageable<ShareFileItem> items = client.GetFilesAndDirectories();
            foreach (ShareFileItem item in items)
            {
                if (item.IsDirectory)
                {
                    string subFolder = $"{localFolder}/{item.Name}";
                    Directory.CreateDirectory(subFolder);
                    Sync(subFolder, client.GetSubdirectoryClient(item.Name));
                }
                else
                {
                    string filePath = $"{localFolder}/{item.Name}";

                    ShareFileClient file = client.GetFileClient(item.Name);
                    ShareFileDownloadInfo download = file.Download();

                    using (FileStream stream = File.Create(filePath))
                    {
                        download.Content.CopyTo(stream);
                    }
                }
            }
        }

        internal class SyncConfig
        { 
            public string FileShareName { get; set; }
            public string ConnectionString { get; set; }
            public string LocalPath { get; set; }
            public List<string> Excludes { get; set; }
        }
    }
}
