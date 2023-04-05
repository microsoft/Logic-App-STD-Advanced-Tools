## Introduction
This tool integrated several useful features for Logic App Standard which not available in Logic App portal yet.<br/>
Please use command "**LogicAppAdvancedTool -?**" for more information of the commands.


## How to get application binary
You can directly download via "Release" link.
![image](https://user-images.githubusercontent.com/72241569/229997619-fb431ac9-fbfe-47da-82a4-ed37a0be3258.png)

If you would like to compile the binary yourself, please always use "Publish" in Visual Studio, otherwise the DLLs will not be integrated into the exe.

## How to use (Sample of restore a workflow)
1. Open Kudu (Advanced Tools) of Logic App Standard and upload this tool into a folder
![image](https://user-images.githubusercontent.com/72241569/230000172-99d7ad05-fd51-4917-9bc7-47d61cc7ccb6.png)


2. Use command "**LogicAppAdvancedTool ListWorkflows -la [LogicAppName]**" to list all the workflows which can be found in storage table, if you don't remember which one need to be restored.
![image](https://user-images.githubusercontent.com/72241569/230001038-b91892f3-bcc8-4eb1-b3e7-cea6010d79e4.png)


3. Use command "**LogicAppAdvancedTool RestoreSingleWorkflow -la [LogicAppName] -wf [WorkflowName]**" to restore the specific workflow
![image](https://user-images.githubusercontent.com/72241569/230001799-e0d04308-d024-4ea4-bc14-3d74f3dbc37e.png)


4. Refresh Logic App - Workflow page, we can find the deleted workflow has been recovered.


## Limitation
1. This tool only modify workflow.json, if the API connection metadata get lost in connections.json, the reverted workflow will not work.
2. If the definition not be used in 90 days, the backend service will remove it from storage table, so this tool will not be able to retrieve the definitions older than 90 days.
3. Before execute Revert command, we need to backup first since the Revert command is reading workflow definitions from backup folder.


## Release Note
2023-04-05
1. Added warning message for RestoreAll command since if there's any invalid workflows in storage table, it might cause unexpected behavior for Logic App runtime.
2. Added a new command "ListWorkflows" to list all the existing workflows in storage table.
3. Added a new command "RestoreSingleWorkflow" to only restore a particular workflow.

2023-03-02
1. Added command description.
2. Added an experimental command - "IngestWorkflow" which could bypass workflow validation and ingest workflow definition in Storage Table. In some situation, the worklfow definition could be failed, but the definiton still can work as expected (eg: using expression for dynamic assign API connection).

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
