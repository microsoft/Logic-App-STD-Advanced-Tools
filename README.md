# LAVersionReverter  
**Command**  
Backup: Retrieve all definitions for all the workflows and save to json files. Each workflow will be a separate folder and each definition will be a separate json file. All of them are placed in Backup folder in same path of the tool.  

Revert: Revert to a pervious version, it will also do backup  

Clone: Clone a workflow (latest version) to a new workflow, if the workflow already exists, then it will skip.  

Decode: Print identical workflow definition in Kudu console.  

**Command and Options**  
Backup  -cs|--connectionstring [The Connection String of storage account]  

Revert -cs|--connectionstring [ConnectionString] -n|--name [Workflow Name] -v|--version [Definition Version]  

Clone -cs|--connectionstring [ConnectionString] -sn|--sourcename [Source Workflow Name] -ts|--targetname [Target Workflow Name]  

Decode -cs|--connectionstring [ConnectionString] -n|--name [Workflow Name] -v|--version [Definition Version]  

  
Help information also available by -? and [Command] -?.
