using Newtonsoft.Json;

namespace LogicAppAdvancedTool.Structures
{
    public class IPRule
    {
        public IPRule() { }

        public IPRule(string value)
        {
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            IPRule rule = obj as IPRule;
            if ((System.Object)rule == null)
            {
                return false;
            }

            return this.value == rule.value;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public string value { get; set; }

        [JsonProperty("ipMask")]
        private string ipMask { set { this.value = value; } }
    }

    public class RegisteredProvider
    {
        public string APIVersion { get; set; }
        public string UrlParameter { get; set; }
        public string MaskName { get; set; }
        public string RulePath { get; set; }

        public RegisteredProvider() { }
    }
}
