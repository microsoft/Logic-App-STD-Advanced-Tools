using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace LAVersionReverter
{
    class Program
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
                    CommandOption ConnectionStringCO = c.Option("-cs|--connectionString", "The ConnectionString of Logic App's Storage Account", CommandOptionType.SingleValue);
                    c.HelpOption("-?");

                    c.OnExecute(() =>
                    {
                        string ConnectionString = ConnectionStringCO.Value();
                        BackupDefinitions(ConnectionString);

                        Console.WriteLine("Backup Succeeded. You can download the definition ");

                        return 0;
                    });
                });
                #endregion

                #region Revert
                app.Command("Revert", c =>
                {
                    CommandOption ConnectionStringCO = c.Option("-cs|--connectionString", "The ConnectionString of Logic App's Storage Account", CommandOptionType.SingleValue);
                    CommandOption WorkflowNameCO = c.Option("-n|--name", "Workflow Name", CommandOptionType.SingleValue);
                    CommandOption VersionCO = c.Option("-v|--version", "Version, the first part of the backup file name", CommandOptionType.SingleValue);

                    c.HelpOption("-?");
                    c.OnExecute(() =>
                    {
                        string ConnectionString = ConnectionStringCO.Value();
                        string WorkflowName = WorkflowNameCO.Value();
                        string Version = VersionCO.Value();

                        BackupDefinitions(ConnectionString);
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
                    CommandOption ConnectionStringCO = c.Option("-cs|--connectionString", "The ConnectionString of Logic App's Storage Account", CommandOptionType.SingleValue);
                    CommandOption WorkflowNameCO = c.Option("-n|--name", "Workflow Name", CommandOptionType.SingleValue);
                    CommandOption VersionCO = c.Option("-v|--version", "Version, the first part of the backup file name", CommandOptionType.SingleValue);

                    c.HelpOption("-?");
                    c.OnExecute(() =>
                    {
                        string ConnectionString = ConnectionStringCO.Value();
                        string WorkflowName = WorkflowNameCO.Value();
                        string Version = VersionCO.Value();

                        Decode(ConnectionString, WorkflowName, Version);

                        return 0;
                    });
                });
                #endregion

                #region Clone
                app.Command("Clone", c =>
                {
                    CommandOption ConnectionStringCO = c.Option("-cs|--connectionString", "The ConnectionString of Logic App's Storage Account", CommandOptionType.SingleValue);
                    CommandOption SourceNameCO = c.Option("-sn|--sourcename", "Source Workflow Name", CommandOptionType.SingleValue);
                    CommandOption TargetNameCO = c.Option("-tn|--targetname", "Target Workflow Name", CommandOptionType.SingleValue);
                    CommandOption versionCO = c.Option("-v|--version", "Version of the workflow (optional, the latest version will be cloned if not provided this parameter)", CommandOptionType.SingleValue);

                    c.HelpOption("-?");
                    c.OnExecute(() =>
                    {
                        string ConnectionString = ConnectionStringCO.Value();
                        string SourceName = SourceNameCO.Value();
                        string TargetName = TargetNameCO.Value();
                        string Version = versionCO.Value();

                        Clone(ConnectionString, SourceName, TargetName, Version);

                        return 0;
                    });
                });
                #endregion

                #region List versions
                app.Command("ListVersions", c =>
                {
                    CommandOption ConnectionStringCO = c.Option("-cs|--connectionString", "The ConnectionString of Logic App's Storage Account", CommandOptionType.SingleValue);
                    CommandOption WorkflowNameCO = c.Option("-n|--name", "Workflow Name", CommandOptionType.SingleValue);
                    c.HelpOption("-?");

                    c.OnExecute(() => 
                    {
                        string ConnectionString = ConnectionStringCO.Value();
                        string WorkflowName = WorkflowNameCO.Value();
                        ListVersions(ConnectionString, WorkflowName);

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

        private static void ListVersions(string ConnectionString, string WorkflowName)
        {
            string TableName = GetMainTableName(ConnectionString);

            TableClient tableClient = new TableClient(ConnectionString, TableName);
            Pageable<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: $"FlowName eq '{WorkflowName}'");

            foreach (TableEntity entity in tableEntities)
            {
                string RowKey = entity.GetString("RowKey");

                if (RowKey.Contains("FLOWVERSION"))
                {
                    string Version = entity.GetString("FlowSequenceId");
                    DateTimeOffset? UpdateTime = entity.GetDateTimeOffset("FlowUpdatedTime");

                    Console.WriteLine($"Version ID:{Version}    UpdateTime:{UpdateTime}");
                }
            }
        }

        private static void Decode(string ConnectionString, string WorkflowName, string Version)
        {
            string TableName = GetMainTableName(ConnectionString);

            TableClient tableClient = new TableClient(ConnectionString, TableName);
            Pageable<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: $"FlowName eq '{WorkflowName}' and FlowSequenceId eq '{Version}'");

            if (tableEntities.Count<TableEntity>() == 0)
            {
                Console.WriteLine("No Record Found! Please check the Workflow name and the Version(FlowSequenceId)");
                return;
            }

            string Content = String.Empty;

            foreach (TableEntity entity in tableEntities)
            {
                byte[] DefinitionCompressed = entity.GetBinary("DefinitionCompressed");
                string DecompressedDefinition = DecompressContent(DefinitionCompressed);

                dynamic JsonObject = JsonConvert.DeserializeObject(DecompressedDefinition);
                string FormattedContent = JsonConvert.SerializeObject(JsonObject, Formatting.Indented);

                Console.Write(FormattedContent);

                break;
            }
        }

        /// <summary>
        /// Clone a workflow definition (also can be a old version) to a new one
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <param name="SourceName"></param>
        /// <param name="TargetName"></param>
        /// <param name="Version"></param>
        private static void Clone(string ConnectionString, string SourceName, string TargetName, string Version)
        {
            string TableName = GetMainTableName(ConnectionString);

            TableClient tableClient = new TableClient(ConnectionString, TableName);
            Pageable<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: $"FlowName eq '{SourceName}'");

            string Content = String.Empty;

            string Identity = string.IsNullOrEmpty(Version) ? "FLOWIDENTIFIER" : Version;

            foreach (TableEntity entity in tableEntities)
            {
                string RowKey = entity.GetString("RowKey");

                if (RowKey.Contains(Identity))
                {
                    byte[] DefinitionCompressed = entity.GetBinary("DefinitionCompressed");
                    string Kind = entity.GetString("Kind");
                    string DecompressedDefinition = DecompressContent(DefinitionCompressed);

                    string OutputContent = $"{{\"definition\": {DecompressedDefinition},\"kind\": \"{Kind}\"}}";
                    string ClonePath = $"C:/home/site/wwwroot/{TargetName}";

                    if (Directory.Exists(ClonePath))
                    {
                        Console.WriteLine("Workflow already exists, workflow will not be cloned. Please use another target name.");
                    }

                    Directory.CreateDirectory(ClonePath);
                    File.WriteAllText($"{ClonePath}/workflow.json", OutputContent);

                    break;
                }
            }

            Console.WriteLine("Clone finished, please refresh workflow page");
        }

        /// <summary>
        /// Retrieve the table name which contains all the workflow definitions
        /// Need figure out where we store the Logic App ID
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <returns></returns>
        private static string GetMainTableName(string ConnectionString)
        {
            TableServiceClient serviceClient = new TableServiceClient(ConnectionString);
            Pageable<TableItem> results = serviceClient.Query();
            foreach (TableItem ti in results)
            {
                //Looks like this table contains all the definition, need double confirm with PG
                if (ti.Name.StartsWith("flow") && ti.Name.EndsWith("flows") && ti.Name.Length == 24)
                {
                    return ti.Name;
                }
            }

            return string.Empty;
        }

        private static void RevertVersion(string WorkflowName, string Version)
        {
            string BackupFilePath = $"{Directory.GetCurrentDirectory()}/Backup/{WorkflowName}";
            string[] Files = Directory.GetFiles(BackupFilePath, $"*{Version}.json");

            if (Files == null || Files.Length == 0)
            {
                Console.WriteLine("No backup file found, please check the name and version of workflow");
            }

            string BackupDefinitionContent = File.ReadAllText(Files[0]);
            string DefinitionTemplatePath = $"C:/home/site/wwwroot/{WorkflowName}/workflow.json";

            File.WriteAllText(DefinitionTemplatePath, BackupDefinitionContent);

            Console.WriteLine("Revert finished, please refresh the workflow page");
        }

        private static void BackupDefinitions(string ConnectionString)
        {
            string DefinitionTableName = GetMainTableName(ConnectionString);
            string BackupFolder = $"{Directory.GetCurrentDirectory()}/Backup";

            TableClient tableClient = new TableClient(ConnectionString, DefinitionTableName);
            Pageable<TableEntity> tableEntities = tableClient.Query<TableEntity>();

            foreach (TableEntity entity in tableEntities)
            {
                string RowKey = entity.GetString("RowKey");
                string FlowSequenceId = entity.GetString("FlowSequenceId");
                string FlowName = entity.GetString("FlowName");
                string ModifiedDate = ((DateTimeOffset)entity.GetDateTimeOffset("ChangedTime")).ToString("yyyy_MM_dd_HH_mm_ss");

                string BackupFlowPath = $"{BackupFolder}/{FlowName}";
                string BackupFilePath = $"{BackupFlowPath}/{ModifiedDate}_{FlowSequenceId}.json";

                //Filter for duplicate definition which in used
                if (!RowKey.Contains("FLOWVERSION"))
                {
                    continue;
                }

                //The definition has been already backup
                if (File.Exists(BackupFilePath))
                {
                    continue;
                }

                if (!Directory.Exists(BackupFlowPath))
                {
                    Directory.CreateDirectory(BackupFlowPath);
                }

                byte[] DefinitionCompressed = entity.GetBinary("DefinitionCompressed");
                string Kind = entity.GetString("Kind");
                string DecompressedDefinition = DecompressContent(DefinitionCompressed);

                string OutputContent = $"{{\"definition\": {DecompressedDefinition},\"kind\": \"{Kind}\"}}";

                File.WriteAllText(BackupFilePath, OutputContent);
            }
        }

        private static string DecompressContent(byte[] Content)
        {
            string Result = String.Empty;

            MemoryStream output = new MemoryStream();
            using (var compressStream = new MemoryStream(Content))
            {
                using (var decompressor = new DeflateStream(compressStream, CompressionMode.Decompress))
                {
                    decompressor.CopyTo(output);
                }
                output.Position = 0;
            }

            using (StreamReader reader = new StreamReader(output))
            {
                Result = reader.ReadToEnd();
            }

            return Result;
        }

        public class WorkflowDefinition
        {
            public string WorkflowName;
            public string Version;
            public string ModifiedData;
            public string Definition;

            public WorkflowDefinition(string WorkflowName, string Version, string ModifiedData, string Definition)
            {
                this.WorkflowName = WorkflowName;
                this.Version = Version;
                this.ModifiedData = ModifiedData;
                this.Definition = Definition;
            }
        }
    }
}
