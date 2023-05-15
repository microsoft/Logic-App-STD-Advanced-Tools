using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Microsoft.VisualBasic;
using Microsoft.WindowsAzure.ResourceStack.Common.Utilities;
using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace LogicAppAdvancedTool
{
    public partial class Program
    {
        #region Run history action content data structure
        public class ConnectorPayloadStructure
        {
            public NestedContentLinks nestedContentLinks { get; set; }
        }

        public class NestedContentLinks
        {
            public CommonPayloadStructure body { get; set; }
        }

        public class CommonPayloadStructure
        {
            public string inlinedContent { get; set; }
            public string contentVersion { get; set; }
            public int contentSize { get; set; }
            public ContentHash contentHash { get; set; }
        }

        public class ContentHash
        {
            public string algorithm { get; set; }
            public string value { get; set; }
        }

        public class ActionError
        {
            public string code { get; set; }
            public string message { get; set; }
        }
        #endregion

        #region Workflow definition 
        public class WorkflowTemplate
        {
            public object definition { get; set; }
            public string kind { get; set; }
        }
        #endregion

        public class UserInputException : Exception
        {
            public UserInputException(string Message) : base(Message) { }
        }
    }
}
