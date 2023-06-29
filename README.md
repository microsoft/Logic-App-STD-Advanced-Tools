## Introduction
This tool integrated several useful features for Logic App Standard which not available in Logic App portal yet.<br/>
Please use command "**LogicAppAdvancedTool -?/LogicAppAdvancedTool [COMMAND] -?**" for more information of the commands.


## How to get application binary
You can directly download via "Release" link.
![image](https://user-images.githubusercontent.com/72241569/229997619-fb431ac9-fbfe-47da-82a4-ed37a0be3258.png)

If you would like to compile the binary yourself, please always use "Publish" in Visual Studio, otherwise DLLs will not be integrated into the exe.
<br/>


## Commands
| Commands | Description |
| --- | --- |
| Backup | Retrieve all the definitions which can be found in Storage Table and save as Json files. The storage table saves the definition for past 90 days by default even they have been deleted.|
|ClearJobQueue | Clear Logic App storage queue for stopping any running instances, this action could casue data lossing.|
| Clone | Clone a workflow to a new workflow, only support for same Logic App and same kind (stateful or stateless).|
| ConvertToStateful | Clone a stateless workflow and create a new stateful workflow.|
| Decode | Decode a workflow based on provided version to human readable content.|
| GenerateTablePrefix | Generate Logic App/Workflow's storage table prefix.|
| IngestWorkflow | **This is an experimental feature.  NOT fully tested, DON'T use in PROD environment!!!** Ingest a workflow directly into Storage Table directly to bypass workflow definition validation.|
| ListVersions | List all the exisiting versions of a workflow.|
| ListWorkflows | List all the exisiting workflows which can be found in storage table.|
| RestoreAll | Restore all the workflows which be deleted accidentally. **This operation may cause unexpected behavior on Logic App runtime if you have any invalid workflows in storage table**.|
| RestoreSingleWorkflow | Restore a workflows which has been deleted accidentally.|
| RetrieveFailures | Retrieve all the detail failure information of a workflow for a specific day/run.|
| Revert | Revert a workflow to a previous version, this command will backup all the workflows in advance to prevent any unexpected incidents.|
| SyncToLocal | Sync remote wwwroot folder of Logic App Standard to local project. This command must run in local computer. There are 3 subcommands for different usage, please use -? for more information.|
| CheckConnectivity | Check the connection between Logic App and Storage Account via DNS resolution and tcp ping of 443 port. This command need Kudu site is available. |
| GenerateRunHistoryUrl | Generate run history of failure runs of a specific workflow on a specific day. The url can directly open the run history page |
| CleanUpContainer | Delete all the Logic App auto-generated blob containers for run history before a specific date. |
| SearchInHistory | Search a keyword in workflow run history based on date. |

## How to use (Demo of restore a workflow)
1. Open Kudu (Advanced Tools) of Logic App Standard and upload this tool into a folder
![image](https://user-images.githubusercontent.com/72241569/230000172-99d7ad05-fd51-4917-9bc7-47d61cc7ccb6.png)


2. Use command "**LogicAppAdvancedTool ListWorkflows**" to list all the workflows which can be found in storage table, if you don't remember which one need to be restored.
<img alt="image" src="https://github.com/Drac-Zhang/Logic-App-STD-Advanced-Tools/assets/72241569/f2e54c20-87f2-4cbc-b329-c9e77d664da3">



3. Use command "**LogicAppAdvancedTool RestoreSingleWorkflow -la [LogicAppName] -wf [WorkflowName]**" to restore the specific workflow
![image](https://user-images.githubusercontent.com/72241569/230001799-e0d04308-d024-4ea4-bc14-3d74f3dbc37e.png)


4. Refresh Logic App - Workflow page, we can find the deleted workflow has been recovered.


## Limitation
1. This tool only modify workflow.json, if the API connection metadata get lost in connections.json, the reverted workflow will not work.
2. By default, if the definition not be used in 90 days, the backend service will remove it from storage table which means the tool will not be able to find the definition.
3. Before execute Revert command, we need to backup first since the Revert command is reading workflow definitions from backup folder.
4. Recently "SearchInHistory" command will not include the input/output content/payload which saved in Blob Storage.
