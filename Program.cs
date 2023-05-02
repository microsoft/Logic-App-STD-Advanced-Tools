using Azure;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

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
                    CommandOption LogicAppNameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue).IsRequired();
                    CommandOption AgoCO = c.Option("-ago|--ago", "(Optional) Only retrieve the past X(unsigned integer) days workflow definitions, if not provided then retrieve all existing definitions", CommandOptionType.SingleValue);
                    
                    c.HelpOption("-?");
                    c.Description = "Retrieve all the existing defitnions from Storage Table and save as Json files. The storage table saves the definition for past 90 days by default even they have been deleted.";

                    c.OnExecute(() =>
                    {
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

                        BackupDefinitions(LogicAppName, Ago);

                        Console.WriteLine("Backup Succeeded. You can download the definition.");

                        return 0;
                    });
                });
                #endregion

                #region Revert
                app.Command("Revert", c =>
                {
                    CommandOption LogicAppNameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue).IsRequired();
                    CommandOption WorkflowNameCO = c.Option("-wf|--workflow", "(Mandatory) Workflow Name", CommandOptionType.SingleValue).IsRequired();
                    CommandOption VersionCO = c.Option("-v|--version", "(Mandatory) Version, the first part of the backup file name", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Revert a workflow to a previous version, this command will backup all the workflows in advance to prevent any unexpected incidents.";

                    c.OnExecute(() =>
                    {
                        string LogicAppName = LogicAppNameCO.Value();
                        string WorkflowName = WorkflowNameCO.Value();
                        string Version = VersionCO.Value();

                        BackupDefinitions(LogicAppName, 0);
                        
                        RevertVersion(WorkflowName, Version);

                        return 0;
                    });
                });
                #endregion

                #region Decode
                app.Command("Decode", c =>
                {
                    CommandOption LogicAppNameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue).IsRequired();
                    CommandOption WorkflowNameCO = c.Option("-wf|--workflow", "(Mandatory) Workflow Name", CommandOptionType.SingleValue).IsRequired();
                    CommandOption VersionCO = c.Option("-v|--version", "(Mandatory) Version, the first part of the backup file name", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Decode a workflow based on the version to human readable content.";

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
                    CommandOption LogicAppNameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue).IsRequired();
                    CommandOption SourceNameCO = c.Option("-sn|--sourcename", "(Mandatory) Source Workflow Name", CommandOptionType.SingleValue).IsRequired();
                    CommandOption TargetNameCO = c.Option("-tn|--targetname", "(Mandatory) Target Workflow Name", CommandOptionType.SingleValue).IsRequired();
                    CommandOption versionCO = c.Option("-v|--version", "(Optional) Version of the workflow the latest version will be cloned, if not provided the latest version will be selected.)", CommandOptionType.SingleValue);

                    c.HelpOption("-?");
                    c.Description = "Clone a workflow to a new workflow, only support for same Logic App.";

                    c.OnExecute(() =>
                    {
                        string LogicAppName = LogicAppNameCO.Value();
                        string SourceName = SourceNameCO.Value();
                        string TargetName = TargetNameCO.Value();
                        string Version = versionCO.Value();

                        Clone(LogicAppName, SourceName, TargetName, Version);

                        return 0;
                    });
                });
                #endregion

                #region List versions
                app.Command("ListVersions", c =>
                {
                    CommandOption LogicAppNameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue).IsRequired();
                    CommandOption WorkflowNameCO = c.Option("-wf|--workflow", "(Mandatory) Workflow Name", CommandOptionType.SingleValue).IsRequired();
                    
                    c.HelpOption("-?");
                    c.Description = "List all the exisiting versions of a workflow.";

                    c.OnExecute(() => 
                    {
                        string LogicAppName = LogicAppNameCO.Value();
                        string WorkflowName = WorkflowNameCO.Value();

                        ListVersions(LogicAppName, WorkflowName);

                        return 0;
                    });
                });
                #endregion

                #region Restore All workflows
                app.Command("RestoreAll", c =>
                {
                    CommandOption LogicAppNameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Restore all the workflows which be deleted accidentally.";

                    c.OnExecute(() =>
                    {
                        string LogicAppName = LogicAppNameCO.Value();

                        RestoreAll(LogicAppName);

                        return 0;
                    });
                });
                #endregion

                #region Convert Logic App Name and Workflow Name to it's Storage Table prefix
                app.Command("GenerateTablePrefix", c =>
                {
                    CommandOption LogicAppnameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue).IsRequired();
                    CommandOption WorkflowCO = c.Option("-wf|--workflow", "(Optional) Workflow name, if not provided, only Logic App prefix will be generated)", CommandOptionType.SingleValue);

                    c.HelpOption("-?");
                    c.Description = "Generate Logic App/Workflow's storage table prefix.";

                    c.OnExecute(() => 
                    {
                        string LogicAppName = LogicAppnameCO.Value();
                        string WorkflowName = WorkflowCO.Value();

                        GenerateTablePrefix(LogicAppName, WorkflowName);

                        return 0;
                    });
                });
                #endregion

                #region Retrieve Failure Logs
                app.Command("RetrieveFailures", c =>
                {
                    CommandOption LogicAppnameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue).IsRequired();
                    CommandOption WorkflowCO = c.Option("-wf|--workflow", "(Mandatory) Workflow name", CommandOptionType.SingleValue).IsRequired();
                    CommandOption DateCO = c.Option("-d|--date", "(Mandatory) Date (format: \"yyyyMMdd\") of the logs need to be retrieved, UTC time", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Retrieve all the detail failure information of a workflow for a specific day.";

                    c.OnExecute(() =>
                    {
                        string LogicAppName = LogicAppnameCO.Value();
                        string WorkflowName = WorkflowCO.Value();
                        string Date = DateCO.Value();

                        RetrieveFailures(LogicAppName, WorkflowName, Date);

                        return 0;
                    });
                });
                #endregion

                #region Stateless to Stateful
                app.Command("ConvertToStateful", c =>
                {
                    CommandOption LogicAppNameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue).IsRequired();
                    CommandOption SourceNameCO = c.Option("-sn|--sourcename", "(Mandatory) Source Workflow Name (Stateless)", CommandOptionType.SingleValue).IsRequired();
                    CommandOption TargetNameCO = c.Option("-tn|--targetname", "(Mandatory) Target Workflow Name (Stateful)", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Clone a stateless workflow and create a new stateful workflow.";

                    c.OnExecute(() =>
                    {
                        string LogicAppName = LogicAppNameCO.Value();
                        string SourceName = SourceNameCO.Value();
                        string TargetName = TargetNameCO.Value();

                        ConvertToStateful(LogicAppName, SourceName, TargetName);

                        return 0;
                    });
                });
                #endregion

                #region Clear Queue
                app.Command("ClearJobQueue", c => {
                    CommandOption LogicAppNameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Clear Logic App storage queue for stopping any running instances, this action could casue data lossing.";

                    c.OnExecute(() =>
                    {
                        string LogicAppName = LogicAppNameCO.Value();

                        ClearJobQueue(LogicAppName);

                        return 0;
                    });
                });
                #endregion

                #region Restore single workflow
                app.Command("RestoreSingleWorkflow", c =>
                {
                    CommandOption LogicAppNameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue).IsRequired();
                    CommandOption WorkflowNameCO = c.Option("-wf|--workflow", "(Mandatory) The name of the workflow", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Restore a workflows which has been deleted accidentally.";

                    c.OnExecute(() =>
                    {
                        string LogicAppName = LogicAppNameCO.Value();
                        string WorkflowName = WorkflowNameCO.Value();

                        RestoreSingleWorkflow(LogicAppName, WorkflowName);

                        return 0;
                    });
                });
                #endregion

                #region List Workflows
                app.Command("ListWorkflows", c =>
                {
                    CommandOption LogicAppNameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "List all the exisiting workflows which can be found in storage table.";

                    c.OnExecute(() =>
                    {
                        string LogicAppName = LogicAppNameCO.Value();

                        ListWorkflows(LogicAppName);

                        return 0;
                    });
                });
                #endregion

                #region Sync to local
                app.Command("SyncToLocal", c => {

                    c.HelpOption("-?");
                    c.Description = "Sync remote wwwroot folder of Logic App Standard to local project. This command must run in local computer.";

                    #region Normal Mode
                    c.Command("Normal", sub =>
                    {
                        sub.HelpOption("-?");
                        sub.Description = "Normal mode for manual sync, provides prompt dialog for confirmation of each step.";

                        CommandOption ShareNameCO = sub.Option("-sn|--shareName", "(Mandatory) File Share name of Loigc App storage account", CommandOptionType.SingleValue).IsRequired();
                        CommandOption ConnectionStringCO = sub.Option("-cs|--connectionString", "(Mandatory) Connection string of the File Share", CommandOptionType.SingleValue).IsRequired();
                        CommandOption LocalPathCO = sub.Option("-path|--localPath", "(Mandatory) Destination folder path on your local disk", CommandOptionType.SingleValue).IsRequired();

                        sub.OnExecute(() =>
                        {
                            string ShareName = ShareNameCO.Value();
                            string ConnectionString = ConnectionStringCO.Value();
                            string LocalPath = LocalPathCO.Value();

                            SyncToLocal(ShareName, ConnectionString, LocalPath);

                            return 0;
                        });
                    });
                    #endregion

                    #region Auto Mode
                    c.Command("Auto", sub =>
                    {
                        sub.HelpOption("-?");
                        sub.Description = "Auto mode, there's no prompt dialog and can be set as schedule task for regular execution.";

                        CommandOption ShareNameCO = sub.Option("-sn|--shareName", "(Mandatory) File Share name of Loigc App storage account", CommandOptionType.SingleValue).IsRequired();
                        CommandOption ConnectionStringCO = sub.Option("-cs|--connectionString", "(Mandatory) Connection string of the File Share", CommandOptionType.SingleValue).IsRequired();
                        CommandOption LocalPathCO = sub.Option("-path|--localPath", "(Mandatory) Destination folder path on your local disk", CommandOptionType.SingleValue).IsRequired();
                        CommandOption ExcludesCO = sub.Option("-ex|--excludes", "(Optional) The folders which need to be excluded (use comma for split), .git, .vscode will be excluded by default.", CommandOptionType.SingleValue);

                        sub.OnExecute(() =>
                        {
                            string ShareName = ShareNameCO.Value();
                            string ConnectionString = ConnectionStringCO.Value();
                            string LocalPath = LocalPathCO.Value();
                            string Excludes = ExcludesCO.Value();

                            List<string> ExcludeItems = new List<string>();
                            if (!string.IsNullOrEmpty(Excludes))
                            { 
                                ExcludeItems = Excludes.Split(',').ToList();
                            }

                            AutoSyncToLocal(ShareName, ConnectionString, LocalPath, ExcludeItems);

                            return 0;
                        });
                    });
                    #endregion

                    #region Batch Mode
                    c.Command("Batch", sub =>
                    {
                        sub.HelpOption("-?");
                        sub.Description = "Batch mode, read configuration file (JSON format) from local folder and sync all the Logic Apps which provided in config without prompt confirmation dialog.";

                        CommandOption ConfigFileCO = sub.Option("-cf|--configFile", "(Mandatory) The local configuration file for application to read sync information. Reference can be found on github - sample/BatchSync_SampleConfig.json", CommandOptionType.SingleValue).IsRequired();

                        sub.OnExecute(() =>
                        {
                            string ConfigFile = ConfigFileCO.Value();

                            BatchSyncToLocal(ConfigFile);

                            return 0;
                        });
                    });
                    #endregion

                    c.OnExecute(() =>
                    {
                        throw new UserInputException("Please provide sub command, use -? for help.");
                    });
                });
                #endregion

                #region Check Connectivity
                app.Command("CheckConnectivity", c =>
                {
                    CommandOption LogicAppNameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Check the connectivity between Logic App STD and it's Storage Account";

                    c.OnExecute(() =>
                    {
                        string LogicAppName = LogicAppNameCO.Value();

                        CheckConnectivity(LogicAppName);

                        return 0;
                    });
                });
                #endregion

                #region Ingest workflow
                app.Command("IngestWorkflow", c => {
                    CommandOption LogicAppNameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue).IsRequired();
                    CommandOption WorkflowCO = c.Option("-wf|--workflow", "(Mandatory) The name of workflow", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Ingest a workflow directly into Storage Table directly to bypass workflow definition validation. NOT fully tested, DON'T use in PROD environment.";

                    c.OnExecute(() =>
                    {
                        string LogicAppName = LogicAppNameCO.Value();
                        string WorkflowName = WorkflowCO.Value();

                        IngestWorkflow(LogicAppName, WorkflowName);

                        return 0;
                    });
                });
                #endregion

                app.Execute(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                if (!(ex is UserInputException))     //Print stack trace if it is not related to user input
                {
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }
    }
}