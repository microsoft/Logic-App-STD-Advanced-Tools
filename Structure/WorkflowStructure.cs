using Azure.Data.Tables;
using Newtonsoft.Json;
using System;

namespace LogicAppAdvancedTool
{
    public partial class Program
    {
        public class WorkflowTemplate
        {
            public object definition { get; set; }
            public string kind { get; set; }
        }
    }
}
