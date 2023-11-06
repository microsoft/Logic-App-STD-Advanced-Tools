using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace LogicAppAdvancedTool
{
    internal partial class Tools
    {
        public static void ImportAppsettings(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new UserInputException($"File: {filePath} not exists!");
            }

            Console.WriteLine("This command need to be executed in Administrator mode");

            string confirmationMessage = "WARNING!!!\r\nAll existing environment variables will be overwritten.\r\ninput for confirmation:";
            if (!Prompt.GetYesNo(confirmationMessage, false, ConsoleColor.Red))
            {
                throw new UserCanceledException("Operation Cancelled");
            }

            string fileContent = File.ReadAllText(filePath);

            Dictionary<string, string> appSettings = JObject.Parse(fileContent).ToObject<Dictionary<string, string>>();

            foreach (string key in appSettings.Keys)
            { 
                Environment.SetEnvironmentVariable(key, appSettings[key], EnvironmentVariableTarget.Machine);
            }

            Console.WriteLine("All app settings imported");
        }
    }
}
