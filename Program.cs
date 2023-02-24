using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace LogicAppAdvancedTool
{
    public partial class Program
    {
        static void Main(string[] args)
        {
            CommandLineApplication app = new CommandLineApplication();

            app.HelpOption("-?");
            app.Description = "Logic App Standard Management Tool";

            try
            {
                #region Backup
                app.Command("Backup", c =>
                {
                    CommandOption LogicAppNameCO = c.Option("-la|--logicApp", "The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue);
                    CommandOption AgoCO = c.Option("-ago|--ago", "Only retrieve the past X(unsigned integer) days workflow definitions, if not provided then retrieve all existing definitions", CommandOptionType.SingleValue);
                    
                    c.HelpOption("-?");

                    c.OnExecute(() =>
                    {
                        string ConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                        string LogicAppName = LogicAppNameCO.Value();
                        string AgoStr = AgoCO.Value();

                        uint Ago = 0;
                        if (!String.IsNullOrEmpty(AgoStr))
                        {
                            bool ParseSuccess = uint.TryParse(AgoStr, out Ago);

                            if (!ParseSuccess)
                            {
                                Console.WriteLine("Please provide a valide value for ago option");

                                return 0;
                            }
                        }

                        BackupDefinitions(LogicAppName, ConnectionString, Ago);

                        Console.WriteLine("Backup Succeeded. You can download the definition ");

                        return 0;
                    });
                });
                #endregion

                #region Revert
                app.Command("Revert", c =>
                {
                    CommandOption LogicAppNameCO = c.Option("-la|--logicApp", "The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue);
                    CommandOption WorkflowNameCO = c.Option("-n|--name", "Workflow Name", CommandOptionType.SingleValue);
                    CommandOption VersionCO = c.Option("-v|--version", "Version, the first part of the backup file name", CommandOptionType.SingleValue);

                    c.HelpOption("-?");
                    c.OnExecute(() =>
                    {
                        string LogicAppName = LogicAppNameCO.Value();
                        string ConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                        string WorkflowName = WorkflowNameCO.Value();
                        string Version = VersionCO.Value();

                        BackupDefinitions(LogicAppName, ConnectionString, 0);
                        if (WorkflowName != null && Version != null)
                        {
                            RevertVersion(WorkflowName, Version);
                        }

                        return 0;
                    });
                });
                #endregion

                #region Decode
                app.Command("Decode", c =>
                {
                    CommandOption LogicAppNameCO = c.Option("-la|--logicApp", "The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue);
                    CommandOption WorkflowNameCO = c.Option("-n|--name", "Workflow Name", CommandOptionType.SingleValue);
                    CommandOption VersionCO = c.Option("-v|--version", "Version, the first part of the backup file name", CommandOptionType.SingleValue);

                    c.HelpOption("-?");
                    c.OnExecute(() =>
                    {
                        string LogicAppName = LogicAppNameCO.Value();
                        string ConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                        string WorkflowName = WorkflowNameCO.Value();
                        string Version = VersionCO.Value();

                        Decode(LogicAppName, ConnectionString, WorkflowName, Version);

                        return 0;
                    });
                });
                #endregion

                #region Clone
                app.Command("Clone", c =>
                {
                    CommandOption LogicAppNameCO = c.Option("-la|--logicApp", "The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue);
                    CommandOption SourceNameCO = c.Option("-sn|--sourcename", "Source Workflow Name", CommandOptionType.SingleValue);
                    CommandOption TargetNameCO = c.Option("-tn|--targetname", "Target Workflow Name", CommandOptionType.SingleValue);
                    CommandOption versionCO = c.Option("-v|--version", "Version of the workflow (optional, the latest version will be cloned if not provided this parameter)", CommandOptionType.SingleValue);

                    c.HelpOption("-?");
                    c.OnExecute(() =>
                    {
                        string LogicAppName = LogicAppNameCO.Value();
                        string ConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                        string SourceName = SourceNameCO.Value();
                        string TargetName = TargetNameCO.Value();
                        string Version = versionCO.Value();

                        Clone(LogicAppName, ConnectionString, SourceName, TargetName, Version);

                        return 0;
                    });
                });
                #endregion

                #region List versions
                app.Command("ListVersions", c =>
                {
                    CommandOption LogicAppNameCO = c.Option("-la|--logicApp", "The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue);
                    CommandOption WorkflowNameCO = c.Option("-n|--name", "Workflow Name", CommandOptionType.SingleValue);
                    c.HelpOption("-?");

                    c.OnExecute(() => 
                    {
                        string LogicAppName = LogicAppNameCO.Value();
                        string ConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                        string WorkflowName = WorkflowNameCO.Value();

                        ListVersions(LogicAppName, ConnectionString, WorkflowName);

                        return 0;
                    });
                });
                #endregion

                #region Restore All workflows
                app.Command("RestoreAll", c =>
                {
                    CommandOption LogicAppNameCO = c.Option("-la|--logicApp", "The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue);

                    c.HelpOption("-?");

                    c.OnExecute(() =>
                    {
                        string LogicAppName = LogicAppNameCO.Value();
                        string ConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

                        RestoreAll(LogicAppName, ConnectionString);

                        return 0;
                    });
                });
                #endregion

                #region Convert Logic App Name and Workflow Name to it's Storage Table prefix
                app.Command("GenerateTablePrefix", c =>
                {
                    CommandOption LogicAppnameCO = c.Option("-la|--logicApp", "The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue);
                    CommandOption WorkflowCO = c.Option("-n|--name", "Workflow name (optional)", CommandOptionType.SingleValue);

                    c.HelpOption("-?");

                    c.OnExecute(() => 
                    {
                        string LogicAppName = LogicAppnameCO.Value();
                        string WorkflowName = WorkflowCO.Value();
                        string ConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

                        GenerateTablePrefix(LogicAppName, WorkflowName, ConnectionString);

                        return 0;
                    });
                });
                #endregion

                #region Retrieve Failure Logs
                app.Command("RetrieveFailures", c =>
                {
                    CommandOption LogicAppnameCO = c.Option("-la|--logicApp", "The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue);
                    CommandOption WorkflowCO = c.Option("-n|--name", "Workflow name (optional)", CommandOptionType.SingleValue);
                    CommandOption DateCO = c.Option("-d|--date", "Date (format: \"yyyyMMdd\") of the logs need to be retrieved, utc time", CommandOptionType.SingleValue);

                    c.HelpOption("-?");

                    c.OnExecute(() =>
                    {
                        string LogicAppName = LogicAppnameCO.Value();
                        string WorkflowName = WorkflowCO.Value();
                        string ConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                        string Date = DateCO.Value();

                        RetrieveFailures(LogicAppName, WorkflowName, ConnectionString, Date);

                        return 0;
                    });
                });
                #endregion

                #region Stateless to Stateful
                app.Command("ConvertToStateful", c =>
                {
                    CommandOption LogicAppNameCO = c.Option("-la|--logicApp", "The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue);
                    CommandOption SourceNameCO = c.Option("-sn|--sourcename", "Source Workflow Name (Stateless)", CommandOptionType.SingleValue);
                    CommandOption TargetNameCO = c.Option("-tn|--targetname", "Target Workflow Name (Stateful)", CommandOptionType.SingleValue);

                    c.HelpOption("-?");
                    c.OnExecute(() =>
                    {
                        string LogicAppName = LogicAppNameCO.Value();
                        string ConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                        string SourceName = SourceNameCO.Value();
                        string TargetName = TargetNameCO.Value();

                        ConvertToStateful(LogicAppName, ConnectionString, SourceName, TargetName);

                        return 0;
                    });
                });
                #endregion

                #region Clear Queue
                app.Command("ClearJobQueue", c => {
                    CommandOption LogicAppNameCO = c.Option("-la|--logicApp", "The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue);

                    c.HelpOption("-?");
                    c.OnExecute(() =>
                    {
                        string LogicAppName = LogicAppNameCO.Value();
                        string ConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

                        ClearJobQueue(LogicAppName, ConnectionString);

                        return 0;
                    });
                });
                #endregion

                #region Export Existing Workflows

                #endregion

                app.Execute(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
