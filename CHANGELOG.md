2024-11-26
1. Fixed a bug which CompressUtility missed method to decompress deflate content.

2024-11-21
1. Removed "RestoreAll" command
2. Added sub command "DecodeZSTD" in "Tools" command for decode binary content which compressed via zstd

2024-11-19
1. Added new method for Storage Table query with pagination for memory saving consideration.
2. Remove "onlyFailures" parameter in "SearchInHistory" command since new query mechanism is faster enough to retrieve all items.
3. Added new method for ZSTD compress.

2024-11-15
1. Update decompress method for ZSTD compatiablity which used by Azure Logic App as ModernCompressionUtility.

2024-09-09
1. "SearchInHistory" command now supports for retrieving histories from a deleted workflow.
2. Added a new sub-command "GeneratePrefix" in command "Tools" for generating prefix on local computer. 

2024-06-10
1. Improved ConsoleTable component for auto-generated index feature.
2. "ListWorkflows" command now provide deeper level list which can list workflow with same name but different flow id, all versions of a specific workflow id.

2024-06-07
1. "RestoreSingleWorkflow" has been deprecated and please use "RestoreWorkflowWithVersion" instead.
2. Improved "Backup" command, for it now will create separate folders based on flow id and provide last modified time for reference.

2024-05-16
1. Added a new command "RestoreWorkflowWithVersion" which can restore deleted workflow by name even it has been overwritten. This new command will replace "RestoreSingleWorkflow" command in later version.

2024-04-30
1. MI token for local enviornment has been moved to a seperated cache file and included in .gitignore for security consideration.
2. Fixed a bug when workflows name contains "-" or "_" cause cannot find workflow in table due to character escape.

2024-04-24
1. Fixed a bug which cause storage validation failure when Logic App Managed Identity cannot retrieve service tags from Azure.
2. MergeRunHistory command now can merge run histories into a different workflow.

2024-04-22
1. Implemented a new shared class for Azure service tag management.

2024-04-11
1. "CleanUpTables" and "CleanUpContainers" now can remove tables/containers belong to deleted workflows.
2. "MergeRunHistory" now supports for merging run histories based on time range.
3. Remove "RestoreRunHistory" command due to we have a new command ("MergeRunHistory") now

2024-04-02
1. Added a new command "MergeRunHistory". When you overwritten a workflow (delete and create a new workflow with same name), the run history will lost for deleted workflow. This command can restore the run histories into the existing one.

2024-01-16
1. Modified code for retrieving run history from blob due to the json schema changed by logic App developer.

2024-01-15
1. "CheckStorageConnectivity" now can detect whether the Storage Account is using private endpoint or not. Request subscription level "Reader" role on Logic App MI.

2024-01-11
1. Code improvement for DNS and socket validation.
2. Rename command "CheckStorageConnectivity" to "ValidateStorageConnectivity".
3. Minor bug fix

2023-12-26
1. Change command "CheckConnectivity" to "CheckStorageConnectivity" to avoid confusion.
2. Added a new command "EndpointValidation" which can validate http(s) endpoint name resolution, connectivity, SSL certifcate issue.

2023-12-14
1. Added a new command "ValidateWorkflows", it will validate workflow definition and provide result.
2. Add async http request method

2023-11-29
1. Added a new command "FilterHostLogs", it can retrieve all error and warning logs from Logic App host logs and generate a new log file in the tool folder.

2023-11-06
1. "BatchResubmit" command now supports a new filter parameter (-s|--status) which can resubmit runs based on execution result (succeeded, failed and cancelled).
2. Fix a bug in "ScanConnections" which could cause unused connections not be removed when there's no API connections and service providers in workflows.

2023-10-25
1. New command "ScanConnections" added which will list all unused connections (API connection and Service Providers) in connections.json.
2. Fixed a bug when Logic App MI doesn't have Logic App Contributor role, "Snapshot" command will not backup any files. Now if MI doesn't have permission, then expcet Appsettings, all other files will be backup.
3. Code improvement, added new namespace to reduce method name conflict and remove unused namespace.

2023-09-19
1. Add a new command "Snapshot" which can create a snapshot or restore from a snapshot. Appsettings and all the files under wwwroot folder can be backup and restore. API connection resources will not be backup, if the workflow was deleted, the workflow itself can be restored, but run history will lost.

2023-09-18
1. Remove "ListWorkflowID" command, migrate the same feature in "ListVersions" command.

2023-09-13
1. Add a new command "RetrieveActionPayload" which will grab all the inputs/outputs of provided action within a specific day (large content saved in storage blob not supported yet).

2023-09-12
1. Add a new command "WhitelistConnectorIP" which can whitelist Logic App managed connector IP range in target service. Only Storage Account, Key Vault and Event Hub supported.

2023-09-05
1. Add a new command "ValidateSPConnectivity" which can verify all the service provider destination connectivity (except SAP, JDBC, FileSystem).
2. Add a new command "BatchResubmit" which can resubmit all failed runs of a specific workflow within provided time peroid. We have a limitation on ARM API side which only can call resubmit API ~50 per 5 minutes, so if we hit the throttling limitation, it will wait for 2 mintues (might wait multiple times if 2 minutes is not enough to refresh execution count) and execute again.

2023-08-01
1. Added alert when execute experimental command.

2023-07-25
1. Added a new command "RestoreRunHistory", in Logic App STD, if we overwritten (delete and create a new one with same name) a workflow, we will lost all the run history. This command is using to restore run history of an overwritten workflow.

2023-07-19
1. Modified "Revert" command mechanism, it will retrieve the definition directly from Storage Table instead from local backup file.

2023-07-18
1. Remove -ago option in "Backup" command, instead added -date option for retrieve definitions after a certain date.

2023-07-07
1. Added new command "CancelRuns" to cancel all running/waiting instances of a workflow.
2. New shared class "TableOperations" for common storage table query.
3. Fixed a bug which causing action input/output is not decoded as expected.
4. Fixed a bug which "date" options ignored in query filter when only "SearchInHistory" set to "OnlyFailure" mode.

2023-06-29
1. Logic App name will be read from Appsettings directly, command option "-la" is not required anymore.

2023-06-28
1. "SearchInHistory" command now can search for only failed runs with command option "-of|--onlyFailures"
2. New command "CleanUpTables" and "CleanUpRunHistory" for cleaning historical run history related files.

2023-06-01
1. "SearchInHistory" now support search keyword in run hhistory which saved as a blob file. Due to memory saving issue, the blob large than 1MB will not be scanned.
2. Added a new class for run history content decode.

2023-05-25
1. Fixed a bug which causing action run history input/output cannot be retrieved for API connection actions.

2023-05-24
1. Added a new command "SearchInHistory" for searching a keyword in workflow run history.
2. Added a new command "ListWorkflowID" to list historical flow id which has the same name as per provided workflow name.

2023-05-15
1. Added new optional parameter "-f|--filter" in "GenerateRunHistoryUrl" command for searching failed run of specific exception message.
2. Change "RetrieveFailures" commmand mechansim, now it need to use sub-command to retrieve by date or run id.
3. Seperate all data structure class to a new file (structure.cs).

2023-05-11
1. Added a new command "CleanUpContainer" to manually delete Logic App run history's blob containers.
2. Added a new command "GenerateRunHistoryUrl" to retrieve failure runs of a workflow and generate run history url which can directly open the workflow run history page.
3. Optimize the code for generating Logic App and workflow table prefix.

2023-05-04
1. Added authentication validation in "CheckConnectivity" command.

2023-05-02
1. Added a new commmand "CheckConnectivity" for validating connectivity between Logic App and Storage Account, need Kudu site is available.

2023-04-12
1. Added subcommands in SyncToLocal for different usage.
2. All the required options now have "Mandatory" mark in option description.
3. Remove AutoSyncToLocal command since it is a subcommand now.

2023-04-10
1. Added "Exclude" option in SyncToLocal command to manually exclude folders which need to be always kept in local project.
2. Added new command "AutoSyncToLocal" to avoid prompt confirmation dialog if you would like to use as a local schedule task.

2023-04-07
1. Improved the exception handle for invalid user input
2. Add a new featue in "SyncToLocal" command, provide an option to determine whehter need to clean workflows in local project which have been delete on cloud.

2023-04-06
1. Added validation for required options.
2. For risky operations (eg: RestoreAll, IngestWorkflow), the current wwwroot folder will be backup as an archive file in tool folder.
3. Added a new command "SyncToLocal" which will retrieve all the files from wwwroot folder on cloud sice to local project.

2023-04-05
1. Added warning message for RestoreAll command since if there's any invalid workflows in storage table, it might cause unexpected behavior for Logic App runtime.
2. Added a new command "ListWorkflows" to list all the existing workflows in storage table.
3. Added a new command "RestoreSingleWorkflow" to only restore a particular workflow.

2023-03-02
1. Added command description.
2. Added an experimental command - "IngestWorkflow" which could bypass workflow validation and ingest workflow definition in Storage Table. In some situation, the workflow definition could be failed, but the definiton still can work as expected (eg: using expression for dynamic assign API connection).

2023-02-24
1. Rename the tool to "LogicAppAdvancedTool" since it contains more features not only workflow verison revert now.

2023-02-23
1. Upgrade to .Net 7.0 due to DeflateStream for compress result to different binary between .Net framewrok 4.7.2 and .Net 7.0 which required by new feature.
2. Remove decompress related code, change to call workflow extension Dll method directly.
3. Backup command now will create pretty print content.

2023-02-07
1. Fixed a bug in RetrieveFailures command which cause parse action input/output failure.

2022-12-27
1. Added prompt confirmation feature for critical operations(Revert, ClearJobQueue).
2. Fixed a bug when backup files are not existing but still execute the REvert command.

2022-12-13
1. The connection string will be read from Appsettings -> AzureWebJobsStorage now. No need to provide connection string anymore.

2022-12-12
1. Fixed when execute "GenerateTablePrefix" command for Logic App only, connection string still need be provided.
2. Added a new command "ClearJobQueue" to clear Logic App job queue. This command can resolve some infinity reboot which caused by action high memory. Before run this command, make sure the Logic App Standard has been stopped. **All the data of running workflow instances will be lost！**

2022-11-12
1. Added "RetrieveFailures" command which can fetch all the failed actions information for a single workflow and specific day.

2022-11-09
1. Added "ConvertToStateful" command to clone a stateless workflow as a stateful workflow. Due to some built-in actions (eg: Service Bus peek-lock related) are not available in stateful workflow, the convert will success but run will fail.

2022-10-28
1. Added "GenerateTablePrefix" command to generate Logic App's table prefix as per Logic App name and workflow name.
2. Added a new option "-ago" in Backup command to only retrieve definitions for past X days.

2022-09-30
1. Added "RestoreAll" command, this new command will retrieve all the definitions which can be found in Storage Table and restore to Logic App Standard.
2. Fixed some typo issues.

2022-08-04
1. Changed the mechanism of retrieving Logic App's definition table name to prevent wrong definition table get picked up if there are multiple Logic App Standard binding the same Storage Account. For all the command, we need to add an extra option **"-la [LogicAppName]"** to identify which Logic App we need to operate. This new option is not case sensetive and only Logic App name is required.
![image](https://user-images.githubusercontent.com/72241569/182770468-5ad3e8af-f990-445e-982d-47e7b338f158.png)
