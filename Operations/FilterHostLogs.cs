using Azure.Data.Tables;
using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LogicAppAdvancedTool.Operations
{
    public static class FilterHostLogs
    {
        public static void Run()
        {
            string logsPath = "C:/home/LogFiles/Application/Functions/Host/";
            string filteredLogPath = $"FilteredLogs_{DateTime.Now.ToString("yyyyMMddHHmmss")}.log";

            string[] fileInfos = Directory.GetFiles(logsPath, "*.log");

            if (fileInfos.Length == 0)
            {
                Console.WriteLine("No log files detected.");

                return;
            }

            int filteredCount = 0;

            foreach (string path in fileInfos)
            {
                Console.WriteLine($"Scanning {path}");

                string[] allLines = File.ReadAllLines(path);

                bool readNext = false;
                StringBuilder filteredContent = new StringBuilder();

                foreach (string line in allLines) 
                {
                    if (line.Contains("[Error]") || line.Contains("[Warning]"))
                    {
                        filteredCount++;
                        filteredContent.AppendLine(line);
                        readNext = true;
                    }
                    else if (line.Contains("[Information]"))
                    {
                        readNext = false;
                    }
                    else
                    {
                        if (readNext)
                        {
                            filteredContent.AppendLine(line);
                        }
                    }
                }

                if (filteredContent.Length > 0)
                {
                    File.AppendAllText(filteredLogPath, $"Error and Warning logs in {path}\r\n");
                    File.AppendAllText(filteredLogPath, filteredContent.ToString());
                    File.AppendAllText(filteredLogPath, "==========================================================\r\n\r\n");
                }
            }

            if (filteredCount == 0)
            {
                Console.WriteLine($"There's no warning or error messages found in current logs.");

                return;
            }

            Console.WriteLine($"All logs filters, please open {filteredLogPath} for detail information.");
        }
    }
}
