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
using System.Net;
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
                    CommandOption logicAppNameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue).IsRequired();
                    CommandOption agoCO = c.Option("-ago|--ago", "(Optional) Only retrieve the past X(unsigned integer) days workflow definitions, if not provided then retrieve all existing definitions", CommandOptionType.SingleValue);
                    
                    c.HelpOption("-?");
                    c.Description = "Retrieve all the existing defitnions from Storage Table and save as Json files. The storage table saves the definition for past 90 days by default even they have been deleted.";

                    c.OnExecute(() =>
                    {
                        string logicAppName = logicAppNameCO.Value();
                        string agoStr = agoCO.Value();

                        uint ago = 0;
                        if (!String.IsNullOrEmpty(agoStr))
                        {
                            bool parseSuccess = uint.TryParse(agoStr, out ago);

                            if (!parseSuccess)
                            {
                                Console.WriteLine("Please provide a valide value for ago option");

                                return 0;
                            }
                        }

                        BackupDefinitions(logicAppName, ago);

                        return 0;
                    });
                });
                #endregion

                #region Revert
                app.Command("Revert", c =>
                {
                    CommandOption logicAppNameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue).IsRequired();
                    CommandOption workflowNameCO = c.Option("-wf|--workflow", "(Mandatory) Workflow Name", CommandOptionType.SingleValue).IsRequired();
                    CommandOption versionCO = c.Option("-v|--version", "(Mandatory) Version, the first part of the backup file name", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Revert a workflow to a previous version, this command will backup all the workflows in advance to prevent any unexpected incidents.";

                    c.OnExecute(() =>
                    {
                        string logicAppName = logicAppNameCO.Value();
                        string workflowName = workflowNameCO.Value();
                        string version = versionCO.Value();

                        BackupDefinitions(logicAppName, 0);
                        
                        RevertVersion(workflowName, version);

                        return 0;
                    });
                });
                #endregion

                #region Decode
                app.Command("Decode", c =>
                {
                    CommandOption logicAppNameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue).IsRequired();
                    CommandOption workflowNameCO = c.Option("-wf|--workflow", "(Mandatory) Workflow Name", CommandOptionType.SingleValue).IsRequired();
                    CommandOption versionCO = c.Option("-v|--version", "(Mandatory) Version, the first part of the backup file name", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Decode a workflow based on the version to human readable content.";

                    c.OnExecute(() =>
                    {
                        string logicAppName = logicAppNameCO.Value();
                        string workflowName = workflowNameCO.Value();
                        string version = versionCO.Value();

                        Decode(logicAppName, workflowName, version);

                        return 0;
                    });
                });
                #endregion

                #region Clone
                app.Command("Clone", c =>
                {
                    CommandOption logicAppNameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue).IsRequired();
                    CommandOption sourceNameCO = c.Option("-sn|--sourcename", "(Mandatory) Source Workflow Name", CommandOptionType.SingleValue).IsRequired();
                    CommandOption targetNameCO = c.Option("-tn|--targetname", "(Mandatory) Target Workflow Name", CommandOptionType.SingleValue).IsRequired();
                    CommandOption versionCO = c.Option("-v|--version", "(Optional) Version of the workflow the latest version will be cloned, if not provided the latest version will be selected.)", CommandOptionType.SingleValue);

                    c.HelpOption("-?");
                    c.Description = "Clone a workflow to a new workflow, only support for same Logic App.";

                    c.OnExecute(() =>
                    {
                        string logicAppName = logicAppNameCO.Value();
                        string sourceName = sourceNameCO.Value();
                        string targetName = targetNameCO.Value();
                        string version = versionCO.Value();

                        Clone(logicAppName, sourceName, targetName, version);

                        return 0;
                    });
                });
                #endregion

                #region List versions
                app.Command("ListVersions", c =>
                {
                    CommandOption logicAppNameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue).IsRequired();
                    CommandOption workflowNameCO = c.Option("-wf|--workflow", "(Mandatory) Workflow Name", CommandOptionType.SingleValue).IsRequired();
                    
                    c.HelpOption("-?");
                    c.Description = "List all the exisiting versions of a workflow.";

                    c.OnExecute(() => 
                    {
                        string logicAppName = logicAppNameCO.Value();
                        string workflowName = workflowNameCO.Value();

                        ListVersions(logicAppName, workflowName);

                        return 0;
                    });
                });
                #endregion

                #region Restore All workflows
                app.Command("RestoreAll", c =>
                {
                    CommandOption logicAppNameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Restore all the workflows which be deleted accidentally.";

                    c.OnExecute(() =>
                    {
                        string logicAppName = logicAppNameCO.Value();

                        RestoreAll(logicAppName);

                        return 0;
                    });
                });
                #endregion

                #region Convert Logic App Name and Workflow Name to it's Storage Table prefix
                app.Command("GenerateTablePrefix", c =>
                {
                    CommandOption logicAppnameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue).IsRequired();
                    CommandOption workflowCO = c.Option("-wf|--workflow", "(Optional) Workflow name, if not provided, only Logic App prefix will be generated)", CommandOptionType.SingleValue);

                    c.HelpOption("-?");
                    c.Description = "Generate Logic App/Workflow's storage table prefix.";

                    c.OnExecute(() => 
                    {
                        string logicAppName = logicAppnameCO.Value();
                        string workflowName = workflowCO.Value();

                        GenerateTablePrefix(logicAppName, workflowName);

                        return 0;
                    });
                });
                #endregion

                #region Retrieve Failure Logs
                app.Command("RetrieveFailures", c =>
                {
                    c.HelpOption("-?");
                    c.Description = "Retrieve all the detail failure information of a workflow";

                    #region Retrieve by date
                    c.Command("Date", sub => {
                        CommandOption logicAppnameCO = sub.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue).IsRequired();
                        CommandOption workflowCO = sub.Option("-wf|--workflow", "(Mandatory) Workflow name", CommandOptionType.SingleValue).IsRequired();
                        CommandOption dateCO = sub.Option("-d|--date", "(Mandatory) Date (format: \"yyyyMMdd\") of the logs need to be retrieved, UTC time", CommandOptionType.SingleValue).IsRequired();

                        sub.HelpOption("-?");
                        sub.Description = "Retrieve all the detail failure information of a workflow for a specific day.";

                        sub.OnExecute(() =>
                        {
                            string logicAppName = logicAppnameCO.Value();
                            string workflowName = workflowCO.Value();
                            string date = dateCO.Value();

                            RetrieveFailuresByDate(logicAppName, workflowName, date);

                            return 0;
                        });
                    });
                    #endregion

                    #region Retrieve by run id
                    c.Command("Run", sub => {
                        CommandOption logicAppnameCO = sub.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue).IsRequired();
                        CommandOption workflowCO = sub.Option("-wf|--workflow", "(Mandatory) Workflow name", CommandOptionType.SingleValue).IsRequired();
                        CommandOption runIDCO = sub.Option("-id|--id", "(Mandatory) The workflow run id", CommandOptionType.SingleValue).IsRequired();

                        sub.HelpOption("-?");
                        sub.Description = "Retrieve all the detail failure information of a workflow for a specific run.";

                        sub.OnExecute(() =>
                        {
                            string logicAppName = logicAppnameCO.Value();
                            string workflowName = workflowCO.Value();
                            string runID = runIDCO.Value();

                            RetrieveFailuresByRun(logicAppName, workflowName, runID);

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

                #region Stateless to Stateful
                app.Command("ConvertToStateful", c =>
                {
                    CommandOption logicAppNameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue).IsRequired();
                    CommandOption sourceNameCO = c.Option("-sn|--sourcename", "(Mandatory) Source Workflow Name (Stateless)", CommandOptionType.SingleValue).IsRequired();
                    CommandOption targetNameCO = c.Option("-tn|--targetname", "(Mandatory) Target Workflow Name (Stateful)", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Clone a stateless workflow and create a new stateful workflow.";

                    c.OnExecute(() =>
                    {
                        string logicAppName = logicAppNameCO.Value();
                        string sourceName = sourceNameCO.Value();
                        string targetName = targetNameCO.Value();

                        ConvertToStateful(logicAppName, sourceName, targetName);

                        return 0;
                    });
                });
                #endregion

                #region Clear Queue
                app.Command("ClearJobQueue", c => {
                    CommandOption logicAppNameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Clear Logic App storage queue for stopping any running instances, this action could casue data lossing.";

                    c.OnExecute(() =>
                    {
                        string logicAppName = logicAppNameCO.Value();

                        ClearJobQueue(logicAppName);

                        return 0;
                    });
                });
                #endregion

                #region Restore single workflow
                app.Command("RestoreSingleWorkflow", c =>
                {
                    CommandOption logicAppNameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue).IsRequired();
                    CommandOption workflowNameCO = c.Option("-wf|--workflow", "(Mandatory) The name of the workflow", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Restore a workflows which has been deleted accidentally.";

                    c.OnExecute(() =>
                    {
                        string logicAppName = logicAppNameCO.Value();
                        string workflowName = workflowNameCO.Value();

                        RestoreSingleWorkflow(logicAppName, workflowName);

                        return 0;
                    });
                });
                #endregion

                #region List Workflows
                app.Command("ListWorkflows", c =>
                {
                    CommandOption logicAppNameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "List all the exisiting workflows which can be found in storage table.";

                    c.OnExecute(() =>
                    {
                        string logicAppName = logicAppNameCO.Value();

                        ListWorkflows(logicAppName);

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

                        CommandOption shareNameCO = sub.Option("-sn|--shareName", "(Mandatory) File Share name of Loigc App storage account", CommandOptionType.SingleValue).IsRequired();
                        CommandOption connectionStringCO = sub.Option("-cs|--connectionString", "(Mandatory) Connection string of the File Share", CommandOptionType.SingleValue).IsRequired();
                        CommandOption localPathCO = sub.Option("-path|--localPath", "(Mandatory) Destination folder path on your local disk", CommandOptionType.SingleValue).IsRequired();

                        sub.OnExecute(() =>
                        {
                            string shareName = shareNameCO.Value();
                            string connectionString = connectionStringCO.Value();
                            string localPath = localPathCO.Value();

                            SyncToLocal(shareName, connectionString, localPath);

                            return 0;
                        });
                    });
                    #endregion

                    #region Auto Mode
                    c.Command("Auto", sub =>
                    {
                        sub.HelpOption("-?");
                        sub.Description = "Auto mode, there's no prompt dialog and can be set as schedule task for regular execution.";

                        CommandOption shareNameCO = sub.Option("-sn|--shareName", "(Mandatory) File Share name of Loigc App storage account", CommandOptionType.SingleValue).IsRequired();
                        CommandOption connectionStringCO = sub.Option("-cs|--connectionString", "(Mandatory) Connection string of the File Share", CommandOptionType.SingleValue).IsRequired();
                        CommandOption localPathCO = sub.Option("-path|--localPath", "(Mandatory) Destination folder path on your local disk", CommandOptionType.SingleValue).IsRequired();
                        CommandOption excludesCO = sub.Option("-ex|--excludes", "(Optional) The folders which need to be excluded (use comma for split), .git, .vscode will be excluded by default.", CommandOptionType.SingleValue);

                        sub.OnExecute(() =>
                        {
                            string shareName = shareNameCO.Value();
                            string connectionString = connectionStringCO.Value();
                            string localPath = localPathCO.Value();
                            string excludes = excludesCO.Value();

                            List<string> excludeItems = new List<string>();
                            if (!string.IsNullOrEmpty(excludes))
                            { 
                                excludeItems = excludes.Split(',').ToList();
                            }

                            AutoSyncToLocal(shareName, connectionString, localPath, excludeItems);

                            return 0;
                        });
                    });
                    #endregion

                    #region Batch Mode
                    c.Command("Batch", sub =>
                    {
                        sub.HelpOption("-?");
                        sub.Description = "Batch mode, read configuration file (JSON format) from local folder and sync all the Logic Apps which provided in config without prompt confirmation dialog.";

                        CommandOption configFileCO = sub.Option("-cf|--configFile", "(Mandatory) The local configuration file for application to read sync information. Reference can be found on github - sample/BatchSync_SampleConfig.json", CommandOptionType.SingleValue).IsRequired();

                        sub.OnExecute(() =>
                        {
                            string configFile = configFileCO.Value();

                            BatchSyncToLocal(configFile);

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
                    CommandOption logicAppNameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Check the connectivity between Logic App STD and it's Storage Account";

                    c.OnExecute(() =>
                    {
                        string logicAppName = logicAppNameCO.Value();

                        CheckConnectivity(logicAppName);

                        return 0;
                    });
                });
                #endregion

                #region Ingest workflow
                app.Command("IngestWorkflow", c => {
                    CommandOption logicAppNameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive)", CommandOptionType.SingleValue).IsRequired();
                    CommandOption workflowCO = c.Option("-wf|--workflow", "(Mandatory) The name of workflow", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Ingest a workflow directly into Storage Table directly to bypass workflow definition validation. NOT fully tested, DON'T use in PROD environment.";

                    c.OnExecute(() =>
                    {
                        string logicAppName = logicAppNameCO.Value();
                        string workflowName = workflowCO.Value();

                        IngestWorkflow(logicAppName, workflowName);

                        return 0;
                    });
                });
                #endregion

                #region GenerateRunHistoryUrl
                app.Command("GenerateRunHistoryUrl", c =>
                {
                    CommandOption logicAppNameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive).", CommandOptionType.SingleValue).IsRequired();
                    CommandOption workflowCO = c.Option("-wf|--workflow", "(Mandatory) The name of workflow.", CommandOptionType.SingleValue).IsRequired();
                    CommandOption dateCO = c.Option("-d|--date", "(Mandatory) The date (format: \"yyyyMMdd\") you would like to retrieve logs, UTC time.", CommandOptionType.SingleValue).IsRequired();
                    CommandOption filterCO = c.Option("-f|--filter", "(Optional) Filter for specific exception messages", CommandOptionType.SingleValue);

                    c.HelpOption("-?");
                    c.Description = "Generate run history of workflow failure runs of a specific day.";

                    c.OnExecute(() =>
                    {
                        string logicAppName = logicAppNameCO.Value();
                        string workflowName = workflowCO.Value();
                        string date = dateCO.Value();
                        string filter = filterCO.Value();

                        GenerateRunHistoryUrl(logicAppName, workflowName, date, filter);

                        return 0;
                    });
                    
                });
                #endregion

                #region CleanUpContainers
                app.Command("CleanUpContainers", c =>
                {
                    CommandOption logicAppNameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive).", CommandOptionType.SingleValue).IsRequired();
                    CommandOption workflowCO = c.Option("-wf|--workflow", "(Optional) The name of workflow. If not provided, then all the workflows container will be deleted.", CommandOptionType.SingleValue);
                    CommandOption dateCO = c.Option("-d|--date", "(Mandatory) Delete containers before this date (format: \"yyyyMMdd\") , UTC time.", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Clean up the blob container of Logic App run history to reduce the Storage Account cost.";

                    c.OnExecute(() =>
                    {
                        string logicAppName = logicAppNameCO.Value();
                        string workflowName = workflowCO.Value();
                        string date = dateCO.Value();

                        CleanUpContainers(logicAppName, workflowName, date);

                        return 0;
                    });
                });
                #endregion

                #region List a workflow with versions
                app.Command("ListWorkflowID", c =>
                {
                    CommandOption logicAppNameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive).", CommandOptionType.SingleValue).IsRequired();
                    CommandOption workflowCO = c.Option("-wf|--workflow", "(Mandatory) The name of workflow.", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "List all the workflows which created before with same name.";

                    c.OnExecute(() =>
                    {
                        string logicAppName = logicAppNameCO.Value();
                        string workflowName = workflowCO.Value();

                        ListWorkflowID(logicAppName, workflowName);

                        return 0;
                    });
                });
                #endregion

                #region Search in run history
                app.Command("SearchInHistory", c =>
                {
                    CommandOption logicAppNameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive).", CommandOptionType.SingleValue).IsRequired();
                    CommandOption workflowCO = c.Option("-wf|--workflow", "(Mandatory) The name of workflow.", CommandOptionType.SingleValue).IsRequired();
                    CommandOption dateCO = c.Option("-d|--date", "(Mandatory) Date (format: \"yyyyMMdd\") of the logs need to be searched, UTC time", CommandOptionType.SingleValue).IsRequired();
                    CommandOption keywordCO = c.Option("-k|--keyword", "(Mandatory) The keyword you would like to search for.", CommandOptionType.SingleValue).IsRequired();
                    CommandOption includeBlobCO = c.Option("-b|--includeBlob", "(Optional) true/false, whether need to include the run history which saved as blob. Only the blob size less than 1MB will be checked due to memory saving.", CommandOptionType.SingleValue);

                    c.HelpOption("-?");
                    c.Description = "Search a keywords in workflow run history";

                    c.OnExecute(() =>
                    {
                        string logicAppName = logicAppNameCO.Value();
                        string workflowName = workflowCO.Value();
                        string date = dateCO.Value();
                        string keyword = keywordCO.Value().Trim();
                        bool includeBlob = false;

                        if (!String.IsNullOrEmpty(includeBlobCO.Value()))
                        { 
                            includeBlob = bool.Parse(includeBlobCO.Value());
                        }

                        if (String.IsNullOrEmpty(keyword))
                        {
                            throw new UserInputException("Keyword cannot be empty");
                        }

                        SearchInHistory(logicAppName, workflowName, date, keyword, includeBlob);

                        return 0;
                    });
                });
                #endregion

                #region Clean up Table
                app.Command("CleanUpTables", c =>
                {
                    CommandOption logicAppNameCO = c.Option("-la|--logicApp", "(Mandatory) The name of Logic App Standard (none case sentsitive).", CommandOptionType.SingleValue).IsRequired();
                    CommandOption workflowCO = c.Option("-wf|--workflow", "(Optional) The name of workflow. If not provided, then all the workflows container will be deleted.", CommandOptionType.SingleValue);
                    CommandOption dateCO = c.Option("-d|--date", "(Mandatory) Delete run history related tables before this date (format: \"yyyyMMdd\") , UTC time.", CommandOptionType.SingleValue).IsRequired();

                    c.HelpOption("-?");
                    c.Description = "Clean up the blob container of Logic App run history to reduce the Storage Account cost.";

                    c.OnExecute(() =>
                    {
                        string logicAppName = logicAppNameCO.Value();
                        string workflowName = workflowCO.Value();
                        string date = dateCO.Value();

                        CleanUpTables(logicAppName, workflowName, date);

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