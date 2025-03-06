using Azure.Data.Tables;
using LogicAppAdvancedTool.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace LogicAppAdvancedTool
{
    internal partial class Tools
    {
        public static void InstanceNameConverter(string name)
        {
            string convertedName = string.Empty;

            if (name.Contains("WebWorkerRole".ToUpper()))
            {
                convertedName = ConvertToPortalName(name);
            }
            else
            { 
                convertedName = ConvertToInstanceName(name);
            }

            Console.WriteLine(convertedName);
        }

        private static string ConvertToPortalName(string name)
        {
            string pd = name.Substring(0, 3);

            string nameWithoutPD = name.Substring(3);
            string size = nameWithoutPD.Split("WEB")[0];

            switch (size)
            {
                case "SMALLDEDICATED":
                    size = "SD";
                    break;
                case "MEDIUMDEDICATED":
                    size = "MD";
                    break;
                case "LARGEDEDICATED":
                    size = "LD";
                    break;
            }

            string index = nameWithoutPD.Split("_").Last();
            index = BaseNConverter.DecToBaseN(36, Int32.Parse(index)).PadLeft(6, '0');

            return $"{pd}{size}WK{index}";
        }

        private static string ConvertToInstanceName(string name)
        {
            string pd = name.Substring(0, 3);
            string size = name.Substring(3, 2);
            string role = name.Substring(5, 2);
            string index = name.Substring(7, 6);

            pd = pd.ToLower();
            switch (size)
            { 
                case "SD":
                    size = "SmallDedicated";
                    break;
                case "MD":
                    size = "MediumDedicated";
                    break;
                case "LD":
                    size = "LargeDedicated";
                    break;      
            }

            index = BaseNConverter.BaseNToDec(36, index).ToString();

            return $"{pd}{size}WebWorkerRole_IN_{index}";
        }
    }
}