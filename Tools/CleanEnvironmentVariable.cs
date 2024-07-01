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
        public static void CleanEnvironmentVariable(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new UserInputException($"File: {filePath} not exists!");
            }

            Console.WriteLine("This command need to run with Administrator mode");

            CommonOperations.PromptConfirmation("All environment variables in appsettings file will be deleted.");

            string fileContent = File.ReadAllText(filePath);

            Dictionary<string, string> appSettings = JObject.Parse(fileContent).ToObject<Dictionary<string, string>>();

            foreach (string key in appSettings.Keys)
            {
                Environment.SetEnvironmentVariable(key, null, EnvironmentVariableTarget.Machine);
            }

            Console.WriteLine("Environment variables have been removed.");
        }
    }
}