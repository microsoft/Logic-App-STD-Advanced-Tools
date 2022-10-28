using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace LAVersionReverter
{
    partial class Program
    {
        static void Main(string[] args)
        {
            CommandLineApplication app = new CommandLineApplication();

            app.HelpOption("-?");
            app.Description = "Logic App Standard Definition Backup Tool";

            try
            {
                #region Backup
                app.Command("Backup", c =>
                {
                    CommandOption LogicAppNameCO = c.Option("-la|--logicApp", "The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue);
                    CommandOption ConnectionStringCO = c.Option("-cs|--connectionString", "The ConnectionString of Logic App's Storage Account", CommandOptionType.SingleValue);
                    
                    c.HelpOption("-?");

                    c.OnExecute(() =>
                    {
                        string ConnectionString = ConnectionStringCO.Value();
                        string LogicAppName = LogicAppNameCO.Value();
                        BackupDefinitions(LogicAppName, ConnectionString);

                        Console.WriteLine("Backup Succeeded. You can download the definition ");

                        return 0;
                    });
                });
                #endregion

                #region Revert
                app.Command("Revert", c =>
                {
                    CommandOption LogicAppNameCO = c.Option("-la|--logicApp", "The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue);
                    CommandOption ConnectionStringCO = c.Option("-cs|--connectionString", "The ConnectionString of Logic App's Storage Account", CommandOptionType.SingleValue);
                    CommandOption WorkflowNameCO = c.Option("-n|--name", "Workflow Name", CommandOptionType.SingleValue);
                    CommandOption VersionCO = c.Option("-v|--version", "Version, the first part of the backup file name", CommandOptionType.SingleValue);

                    c.HelpOption("-?");
                    c.OnExecute(() =>
                    {
                        string LogicAppName = LogicAppNameCO.Value();
                        string ConnectionString = ConnectionStringCO.Value();
                        string WorkflowName = WorkflowNameCO.Value();
                        string Version = VersionCO.Value();

                        BackupDefinitions(LogicAppName, ConnectionString);
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
                    CommandOption ConnectionStringCO = c.Option("-cs|--connectionString", "The ConnectionString of Logic App's Storage Account", CommandOptionType.SingleValue);
                    CommandOption WorkflowNameCO = c.Option("-n|--name", "Workflow Name", CommandOptionType.SingleValue);
                    CommandOption VersionCO = c.Option("-v|--version", "Version, the first part of the backup file name", CommandOptionType.SingleValue);

                    c.HelpOption("-?");
                    c.OnExecute(() =>
                    {
                        string LogicAppName = LogicAppNameCO.Value(); 
                        string ConnectionString = ConnectionStringCO.Value();
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
                    CommandOption ConnectionStringCO = c.Option("-cs|--connectionString", "The ConnectionString of Logic App's Storage Account", CommandOptionType.SingleValue);
                    CommandOption SourceNameCO = c.Option("-sn|--sourcename", "Source Workflow Name", CommandOptionType.SingleValue);
                    CommandOption TargetNameCO = c.Option("-tn|--targetname", "Target Workflow Name", CommandOptionType.SingleValue);
                    CommandOption versionCO = c.Option("-v|--version", "Version of the workflow (optional, the latest version will be cloned if not provided this parameter)", CommandOptionType.SingleValue);

                    c.HelpOption("-?");
                    c.OnExecute(() =>
                    {
                        string LogicAppName = LogicAppNameCO.Value();
                        string ConnectionString = ConnectionStringCO.Value();
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
                    CommandOption ConnectionStringCO = c.Option("-cs|--connectionString", "The ConnectionString of Logic App's Storage Account", CommandOptionType.SingleValue);
                    CommandOption WorkflowNameCO = c.Option("-n|--name", "Workflow Name", CommandOptionType.SingleValue);
                    c.HelpOption("-?");

                    c.OnExecute(() => 
                    {
                        string LogicAppName = LogicAppNameCO.Value();
                        string ConnectionString = ConnectionStringCO.Value();
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
                    CommandOption ConnectionStringCO = c.Option("-cs|--connectionString", "The ConnectionString of Logic App's Storage Account", CommandOptionType.SingleValue);

                    c.HelpOption("-?");

                    c.OnExecute(() =>
                    {
                        string LogicAppName = LogicAppNameCO.Value();
                        string ConnectionString = ConnectionStringCO.Value();

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
                    CommandOption ConnectionStringCO = c.Option("-cs|--connectionString", "The ConnectionString of Logic App's Storage Account", CommandOptionType.SingleValue);

                    c.HelpOption("-?");

                    c.OnExecute(() => 
                    {
                        string LogicAppName = LogicAppnameCO.Value();
                        string WorkflowName = WorkflowCO.Value();
                        string ConnectionString = ConnectionStringCO.Value();

                        GenerateTablePrefix(LogicAppName, WorkflowName, ConnectionString);

                        return 0;
                    });
                });
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
