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

        public class HistoryRecords
        {
            public DateTimeOffset Timestamp { get; private set; }
            public string ActionName { get; private set; }
            public string Code { get; private set; }
            public dynamic InputContent { get; private set; }
            public dynamic OutputContent { get; private set; }
            public ActionError Error { get; private set; }
            public string RepeatItemName { get; private set; }
            public int? RepeatItemIdenx { get; private set; }
            public string ActionRepetitionName { get; private set; }

            [JsonIgnore]
            public CommonPayloadStructure InputsLink { get; private set; }
            [JsonIgnore]
            public CommonPayloadStructure OutputsLink { get; private set; }

            public HistoryRecords(TableEntity tableEntity)
            {
                this.Timestamp = tableEntity.GetDateTimeOffset("Timestamp") ?? DateTimeOffset.MinValue;
                this.ActionName = tableEntity.GetString("ActionName");
                this.Code = tableEntity.GetString("Code");
                this.RepeatItemName = tableEntity.GetString("RepeatItemScopeName");
                this.RepeatItemIdenx = tableEntity.GetInt32("RepeatItemIndex");
                this.ActionRepetitionName = tableEntity.GetString("ActionRepetitionName");

                this.InputContent = DecodeActionPayload(tableEntity.GetBinary("InputsLinkCompressed"));
                this.OutputContent = DecodeActionPayload(tableEntity.GetBinary("OutputsLinkCompressed"));

                string rawError = DecompressContent(tableEntity.GetBinary("Error"));
                this.Error = String.IsNullOrEmpty(rawError) ? null : JsonConvert.DeserializeObject<ActionError>(rawError);
            }
        }
    }
}
