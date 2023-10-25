namespace LogicAppAdvancedTool.Structures
{
    public class MSIToken
    {
        public string access_token { get; set; }
        public string expires_on { get; set; }
        public string resource { get; set; }
        public string token_type { get; set; }
        public string client_id { get; set; }

        public MSIToken() { }
    }
}
