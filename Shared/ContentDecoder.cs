using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using LogicAppAdvancedTool.Structures;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using System.Linq;
using Microsoft.WindowsAzure.ResourceStack.Common.Json;

namespace LogicAppAdvancedTool
{
    public class ContentDecoder
    {
        private readonly string _byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
        public dynamic RawPayload { get; private set; }
        public CommonPayloadStructure PayloadBody { get; private set; }
        public bool IsBlobLink { get; private set; }
        public string BlobUri { get; private set; }
        public string InlinedContent { get; private set; }
        public bool IsEmpty { get; private set; }

        public string ActualContent
        {
            get
            {
                return String.IsNullOrEmpty(InlinedContent) ? BlobUri : InlinedContent;
            }
        }

        public ContentDecoder(byte[] binaryContent)
        {
            string rawContent = CommonOperations.DecompressContent(binaryContent) ?? string.Empty;

            if (!String.IsNullOrEmpty(rawContent))
            {
                RawPayload = JsonConvert.DeserializeObject(rawContent);

                if (RawPayload.nestedContentLinks != null)
                {
                    PayloadBody = ((JObject)RawPayload).ToObject<ConnectorPayloadStructure>().nestedContentLinks.body;
                }
                else
                {
                    PayloadBody = ((JObject)RawPayload).ToObject<CommonPayloadStructure>();
                }

                string Base64Content = PayloadBody.inlinedContent;
                if (!String.IsNullOrEmpty(Base64Content))
                {
                    InlinedContent = Encoding.UTF8.GetString(Convert.FromBase64String(Base64Content));
                }
                else
                {
                    InlinedContent = String.Empty;
                }

                BlobUri = PayloadBody.uri ?? String.Empty;
                IsBlobLink = !String.IsNullOrEmpty(BlobUri);
            }
            else
            {
                IsEmpty = true;
            }
        }

        public bool SearchKeyword(string keyword, bool includeBlob = false)
        {
            if (IsEmpty)
            {
                return false;
            }

            if (!String.IsNullOrEmpty(InlinedContent) && InlinedContent.Contains(keyword))
            {
                return true;
            }

            if (IsBlobLink && includeBlob)
            {
                string contentInBlob = CommonOperations.GetBlobContent(BlobUri, 1024 * 1024);

                if (contentInBlob.StartsWith(_byteOrderMarkUtf8))
                {
                    contentInBlob = contentInBlob.Remove(0, _byteOrderMarkUtf8.Length);
                }

                if (contentInBlob.Contains("$content-type") && contentInBlob.Contains("application/octet-stream"))  //quick and dirty implementation
                {
                    JToken streamContent = JToken.Parse(contentInBlob);
                    contentInBlob = DecodeStreamToString(streamContent);

                    if (String.IsNullOrEmpty(contentInBlob))
                    {
                        return false;
                    }
                }

                if (contentInBlob.Contains(keyword))
                {
                    return true;
                }
            }

            return false;
        }

        public string DecodeStreamToString(JToken token)
        {
            string streamContent = string.Empty;

            int i = token.Count();
             
            foreach (JToken t in token.Children())
            {
                if (t is JProperty && ((JProperty)t).Name == "$content")
                {
                    string content = ((JProperty)t).Value.ToString();

                    return Encoding.UTF8.GetString(Convert.FromBase64String(content));
                }
                else
                {
                    if (t.Count() != 0)
                    {
                        streamContent = DecodeStreamToString(t);

                        if (!string.IsNullOrEmpty(streamContent))
                        {
                            return streamContent;
                        }
                    }
                }
            }

            return streamContent;
        }
    }
}
