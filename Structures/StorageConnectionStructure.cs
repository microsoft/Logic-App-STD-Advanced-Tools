using System.Collections.Generic;

namespace LogicAppAdvancedTool.Structures
{
    public class StorageConnectionInfo
    {
        private Dictionary<string, string> CSInfo;
        public StorageConnectionInfo(string connectionString)
        {
            CSInfo = new Dictionary<string, string>();

            string[] infos = connectionString.Split(";");
            foreach (string info in infos)
            {
                int index = info.IndexOf('=');
                string key = info.Substring(0, index);
                string value = info.Substring(index + 1, info.Length - index - 1);

                CSInfo.Add(key, value);
            }

            BlobEndpoint = $"{AccountName}.blob.{EndpointSuffix}";
            FileEndpoint = $"{AccountName}.file.{EndpointSuffix}";
            QueueEndpoint = $"{AccountName}.queue.{EndpointSuffix}";
            TableEndpoint = $"{AccountName}.table.{EndpointSuffix}";
        }

        public string BlobEndpoint { get; private set; }
        public string FileEndpoint { get; private set; }
        public string QueueEndpoint { get; private set; }
        public string TableEndpoint { get; private set; }

        public string DefaultEndpointsProtocol
        {
            get
            {
                return CSInfo["DefaultEndpointsProtocol"];
            }
        }

        public string AccountName
        {
            get
            {
                return CSInfo["AccountName"];
            }
        }

        public string AccountKey
        {
            get
            {
                return CSInfo["AccountKey"];
            }
        }

        public string EndpointSuffix
        {
            get
            {
                return CSInfo["EndpointSuffix"];
            }
        }
    }
}
