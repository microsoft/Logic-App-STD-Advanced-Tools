using System.Collections.Generic;

namespace LogicAppAdvancedTool.Structures
{
    public class StorageConnectionInfo
    {
        private Dictionary<string, string> ConnectionInfo;
        public StorageServiceType storageType { get; private set; }
        public string Endpoint { get; private set; }

        public StorageConnectionInfo(StorageServiceType storageType)
        {
            //quick and dirty
            this.storageType = storageType;

            if (storageType == StorageServiceType.File)
            {
                FormatFromConnectionString(AppSettings.FileShareConnectionString);
            }
            else
            {
                if (!string.IsNullOrEmpty(AppSettings.ConnectionString))
                {
                    FormatFromConnectionString(AppSettings.ConnectionString);
                }
                else
                {
                    ConnectionInfo = new Dictionary<string, string>();
                    string serviceUri = string.Empty;
                    switch (storageType)
                    {
                        case StorageServiceType.Table:
                            serviceUri = AppSettings.TableServiceUri;
                            break;
                        case StorageServiceType.Blob:
                            serviceUri = AppSettings.BlobServiceUri;
                            break;
                        case StorageServiceType.Queue:
                            serviceUri = AppSettings.QueueServiceUri;
                            break;
                    }

                    Endpoint = serviceUri.Replace("https://", "");

                    string accountName = serviceUri.Split('/')[2].Split('.')[0];
                    ConnectionInfo.Add("AccountName", accountName);
                }
            }
        }

        private void FormatFromConnectionString(string connectionString)
        {
            ConnectionInfo = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(connectionString))
            {
                string[] infos = connectionString.Split(";");
                foreach (string info in infos)
                {
                    int index = info.IndexOf('=');
                    string key = info.Substring(0, index);
                    string value = info.Substring(index + 1, info.Length - index - 1);

                    ConnectionInfo.Add(key, value);
                }

                Endpoint = $"{AccountName}.{storageType.ToString().ToLower()}.{EndpointSuffix}";
            }
        }

        public string DefaultEndpointsProtocol
        {
            get
            {
                return ConnectionInfo["DefaultEndpointsProtocol"];
            }
        }

        public string AccountName
        {
            get
            {
                return ConnectionInfo["AccountName"];
            }
        }

        public string AccountKey
        {
            get
            {
                return ConnectionInfo["AccountKey"];
            }
        }

        public string EndpointSuffix
        {
            get
            {
                return ConnectionInfo["EndpointSuffix"];
            }
        }
    }
}
