using LogicAppAdvancedTool.Structures;
using System;

namespace LogicAppAdvancedTool
{
    internal partial class Tools
    {
        public static void Restart()
        {
            MSIToken token = MSITokenService.RetrieveToken("https://management.azure.com");
            Console.WriteLine("Managed Identity token retrieved");

            string restartUrl = $"{AppSettings.ManagementBaseUrl}/restart?api-version=2018-11-01";

            HttpOperations.HttpRequestWithToken(restartUrl, "POST", null, token.access_token, "Failed to restart Logic App.");

            Console.WriteLine("Logic App has been restarted.");
        }
    }
}
