using Azure;
using Azure.Core;
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
                    c.Description = "Revert a workflow to a previous version, this command will backup all the workflows in advance to prevent any unexpected incidents.";

                    c.OnExecute(() =>
                    {
                        string LogicAppName = LogicAppNameCO.Value();
                        string WorkflowName = WorkflowNameCO.Value();
                        string Version = VersionCO.Value();

                        BackupDefinitions(LogicAppName, 0);
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
                    CommandOption LogicAppNameCO = c.Option("-la|--logicApp", "The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue);
                    CommandOption SourceNameCO = c.Option("-sn|--sourcename", "Source Workflow Name", CommandOptionType.SingleValue);
                    CommandOption TargetNameCO = c.Option("-tn|--targetname", "Target Workflow Name", CommandOptionType.SingleValue);
                    CommandOption versionCO = c.Option("-v|--version", "Version of the workflow (optional, the latest version will be cloned if not provided this parameter)", CommandOptionType.SingleValue);

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
                    CommandOption LogicAppNameCO = c.Option("-la|--logicApp", "The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue);
                    CommandOption WorkflowNameCO = c.Option("-n|--name", "Workflow Name", CommandOptionType.SingleValue);
                    
                    c.HelpOption("-?");
                    c.Description = "List all the exisiting versions of a workflow";

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
                    CommandOption LogicAppNameCO = c.Option("-la|--logicApp", "The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue);

                    c.HelpOption("-?");
                    c.Description = "Restore all the workflows which de deleted accidentally.";

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
                    CommandOption LogicAppnameCO = c.Option("-la|--logicApp", "The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue);
                    CommandOption WorkflowCO = c.Option("-n|--name", "Workflow name (optional)", CommandOptionType.SingleValue);

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
                    CommandOption LogicAppnameCO = c.Option("-la|--logicApp", "The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue);
                    CommandOption WorkflowCO = c.Option("-n|--name", "Workflow name (optional)", CommandOptionType.SingleValue);
                    CommandOption DateCO = c.Option("-d|--date", "Date (format: \"yyyyMMdd\") of the logs need to be retrieved, utc time", CommandOptionType.SingleValue);

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
                    CommandOption LogicAppNameCO = c.Option("-la|--logicApp", "The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue);
                    CommandOption SourceNameCO = c.Option("-sn|--sourcename", "Source Workflow Name (Stateless)", CommandOptionType.SingleValue);
                    CommandOption TargetNameCO = c.Option("-tn|--targetname", "Target Workflow Name (Stateful)", CommandOptionType.SingleValue);

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
                    CommandOption LogicAppNameCO = c.Option("-la|--logicApp", "The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue);

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

                #region !!!TODO - Export Existing Workflows

                #endregion

                #region Ingest workflow
                app.Command("IngestWorkflow", c => {
                    CommandOption LogicAppNameCO = c.Option("-la|--logicApp", "The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue);
                    CommandOption WorkflowCO = c.Option("-wf|--workflow", "The name of workflow", CommandOptionType.SingleValue);

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
                Console.WriteLine(ex.StackTrace);
            }
        }

        public static string ConnectionString
        {
            get 
            { 
                return Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            }
        }
    }
}
