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
            public string uri { get; set; }
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

                this.InputContent = new ContentDecoder(tableEntity.GetBinary("InputsLinkCompressed")).RawPayload;
                this.OutputContent = new ContentDecoder(tableEntity.GetBinary("OutputsLinkCompressed")).RawPayload;

                string rawError = DecompressContent(tableEntity.GetBinary("Error"));
                this.Error = String.IsNullOrEmpty(rawError) ? null : JsonConvert.DeserializeObject<ActionError>(rawError);
            }
        }

        public class StreamHistoryBlob
        {
            [JsonProperty(PropertyName = "$content-type")]
            public string ContentType { get; set; }

            [JsonProperty(PropertyName="$content")]
            public string Content { get; set; }
        }
    }
}
