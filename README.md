## Release Note
2023-02-23
1. Upgrade to .Net 7.0 due to DeflateStream for compress result to different binary between .Net framewrok 4.7.2 and .Net 7.0 which required by new feature.

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

2. Removed binary files, please compile the code locally.

## Introduction
This tool can be used for revert the Logic App Standard workflow's previous version which we don't have this this feature on portal yet.

## How to use
1. Open Kudu (Advanced Tools) of Logic App Standard and upload this tool into a folder
![image](https://user-images.githubusercontent.com/72241569/207210227-bc646632-4e91-4427-94c5-705055945711.png)


2. Use command **LogicAppAdvancedTool Backup -la [LogicAppName]** to backup all the existing workflows. The connection string can be found in Storage Account - Access Key
   After run the command, the tool will create a new folder which called "**Backup**", the sub-folders will be named as workflow name. Each definition will be a seperate json file.
![image](https://user-images.githubusercontent.com/72241569/207209672-5d056475-d210-4079-9011-858660c7010f.png)


3. Recently we have to check the definition manually to see which version we would like to revert to. 
   The version is the last part of the file name.
![image](https://user-images.githubusercontent.com/72241569/139812550-29420c41-ab80-4ccd-ad2e-59a471991ab1.png)

4. Use command **LogicAppAdvancedTool Revert -n [Workflow Name] -v [Version]** to revert to previous version

## Limitation
1. This tool only modify workflow.json, if the API connection metadata get lost in connections.json, the reverted workflow will not work.
2. If the definition not be used in 90 days, the backend service will remove it from storage table, so this tool will not be able to retrieve the definitions older than 90 days.
3. Before execute Revert command, we need to backup first since the Revert command is reading workflow definitions from backup folder.

## Supported Command
1. **Backup**: Backup all the existing definitions into Json files
2. **ClearJobQueue**: Clear all incomplete jobs in the Storage Queue. **Be aware of this command will result to data losing for running workflow instances**
3. **Clone**: Clone a workflow to a new one, exactly the same as clone in Logic App comsumption
4. **ConvertToStateful**: Clone a stateless workflow and create a stateful version
5. **Decode** Decode a difinition into readable content
6. **GenerateTablePrefix** Generate Logic App definition table name as per Logic App name
7. **ListVersions** List all the existing versions of a workflow
8. **Revert** Revert a workflow to previous version as per version ID.
9. **RetrieveFailures(Preview)** Retrieve all the failed actions' input/output for a specific day.
10. **RestoreAll** Retrieve all the exsiting definitions from Storage Table and restore in Logic App.
11. **-?/[command] -?** help of the command
