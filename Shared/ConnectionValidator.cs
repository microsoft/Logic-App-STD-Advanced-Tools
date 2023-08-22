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
    }
}
