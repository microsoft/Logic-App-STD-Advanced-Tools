using Azure.Data.Tables;
using LogicAppAdvancedTool.Structures;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace LogicAppAdvancedTool
{
    internal partial class Tools
    {
        public static void RunIDToDatetime(string runID)
        {
            long reversedTicksInID = long.Parse(runID.Substring(0, 20));

            long ticks = long.MaxValue - reversedTicksInID;

            DateTime dt = new DateTime(ticks, DateTimeKind.Utc);

            Console.WriteLine($"Datetime of RunID {runID} is {dt.ToString("yyyy-MM-ddTHH:mm:ssZ")}");
        }
    }
}