using System.Net;

namespace LogicAppAdvancedTool
{
    public class ValidationInfo
    {
        public string Endpoint;
        public ValidationStatus DNSStatus;
        public ValidationStatus PingStatus;

        public ValidationInfo(string endpoint)
        {
            Endpoint = endpoint;
            DNSStatus = ValidationStatus.NotApplicable;
            PingStatus = ValidationStatus.NotApplicable;
        }
    }

    public class StorageValidationInfo : ValidationInfo
    {
        public StorageType ServiceType;
        public IPAddress[] IPs;
        public ValidationStatus AuthStatus;

        public StorageValidationInfo(string endpoint, StorageType serviceType) : base(endpoint)
        {
            ServiceType = serviceType;
            AuthStatus = ValidationStatus.NotApplicable;
        }

        public string GetIPsAsString()
        {
            if (IPs != null)
            {
                string IPsAsString = "";
                foreach (IPAddress ip in IPs)
                {
                    IPsAsString += ip.ToString() + " ";
                }

                return IPsAsString.TrimEnd();
            }

            return "N/A";
        }
    }

    public enum ValidationStatus
    {
        Succeeded,
        Failed,
        NotApplicable,
        Skipped
    }

    public enum StorageType
    {
        Blob = 1,
        File = 2,
        Queue = 4,
        Table = 8
    }

}
