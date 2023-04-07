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
