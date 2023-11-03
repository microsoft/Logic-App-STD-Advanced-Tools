using Azure.Data.Tables;
using System;
using System.IO;
using System.Linq;

namespace LogicAppAdvancedTool.Operations
{
    public static class ConvertToStateful
    {
        public static void Run(string sourceName, string targetName)
        {
            TableEntity entity = TableOperations.QueryMainTable($"FlowName eq '{sourceName}'")
                                                        .Where(t => t.GetString("RowKey").Contains("FLOWIDENTIFIER"))
                                                        .FirstOrDefault();

            if (entity == null)
            {
                throw new UserInputException($"Workflow: {sourceName} cannot be found in storage table, please check your input.");
            }

            string clonePath = $"{AppSettings.RootFolder}\\{targetName}";

            if (Directory.Exists(clonePath))
            {
                throw new UserInputException("Workflow already exists, workflow will not be cloned. Please use another target name.");
            }

            CommonOperations.SaveDefinition(clonePath, "workflow.json", entity);

            Console.WriteLine("Convert finished, please refresh workflow page");
        }
    }
}
