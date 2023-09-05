using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace LogicAppAdvancedTool
{
    public partial class Program
    {
        public class ValidationInfo
        {
            public string Endpoint;
            public ValidateStatus DNSStatus;
            public ValidateStatus PingStatus;

            public ValidationInfo(string endpoint)
            {
                Endpoint = endpoint;
                DNSStatus = ValidateStatus.NotApplicable;
                PingStatus = ValidateStatus.NotApplicable;
            }
        }

        public class StorageValidationInfo : ValidationInfo
        {
            public StorageType ServiceType;
            public IPAddress[] IPs;
            public ValidateStatus AuthStatus;

            public StorageValidationInfo(string endpoint, StorageType serviceType) : base(endpoint)
            {
                ServiceType = serviceType;
                AuthStatus = ValidateStatus.NotApplicable;
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

        public enum ValidateStatus
        {
            Succeeded,
            Failed,
            NotApplicable
        }
    }
}
