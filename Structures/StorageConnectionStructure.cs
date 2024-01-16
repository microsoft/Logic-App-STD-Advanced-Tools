using System.Collections.Generic;

namespace LogicAppAdvancedTool.Structures
{
    public class StorageConnectionInfo
    {
        private Dictionary<string, string> ConnectionInfo;
        public StorageType storageType { get; private set; }
        public string Endpoint { get; private set; }
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

        public StorageConnectionInfo(string connectionString, StorageType storageType)
        {
            this.storageType = storageType;

            ConnectionInfo = new Dictionary<string, string>();

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
}
