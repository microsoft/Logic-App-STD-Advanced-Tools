using Azure.Storage.Files.Shares.Models;
using Azure.Storage.Files.Shares;
using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.IO;
using Azure;

namespace LogicAppAdvancedTool
{
    partial class Program
    {
        private static void SyncToLocal(string ShareName, string ConnectionString, string LocalPath)
        {
            ShareClient shareClient = new ShareClient(ConnectionString, ShareName);

            ShareDirectoryClient directoryClient = shareClient.GetDirectoryClient("site/wwwroot/");

            string ConfirmationMessage = "WARNING!!!\r\nThis operation will overwrite your local project files.\r\nPlease input for confirmation:";
            if (!Prompt.GetYesNo(ConfirmationMessage, false))
            {
                Console.WriteLine("Operation Cancelled");

                return;
            }

            string SyncModeMessage = "Whether clean up workflows in local project which cannot be found on cloud?\r\n\tYes: Clean up all the subfolders which not in clould (except .git, .vscode).\r\n\tNo: Only overwrite the files which modified on cloud, no files will be deleted.\r\nPlease input for confirmation:";
            if (Prompt.GetYesNo(SyncModeMessage, false))
            {
                List<string> ExcludeFolders = new List<string>() { ".git", ".vscode" };

                DirectoryInfo DI = new DirectoryInfo(LocalPath);
                DirectoryInfo[] SubFolders = DI.GetDirectories();

                foreach (DirectoryInfo SubFolder in SubFolders)
                {
                    if (!ExcludeFolders.Contains(SubFolder.Name))
                    { 
                        Directory.Delete(SubFolder.FullName, true);
                    }
                }
            }

            Sync(LocalPath, directoryClient);
        }

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
    }
}
