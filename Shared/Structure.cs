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
            public NestedContentLinks ContentLinks { get; set; }
        }

        public class NestedContentLinks
        {
            public CommonPayloadStructure Body { get; set; }
        }

        public class CommonPayloadStructure
        {
            public string InlinedContent { get; set; }
            public string ContentVersion { get; set; }
            public int ContentSize { get; set; }
            public ContentHash ContentHash { get; set; }
        }

        public class ContentHash
        {
            public string Algorithm { get; set; }
            public string Value { get; set; }
        }

        public class ActionError
        {
            public string Code { get; set; }
            public string Message { get; set; }
        }
        #endregion

        #region Workflow definition 
        public class WorkflowTemplate
        {
            public object Definition { get; set; }
            public string Kind { get; set; }
        }
        #endregion

        public class UserInputException : Exception
        {
            public UserInputException(string Message) : base(Message) { }
        }
    }
}
