using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicAppAdvancedTool.Shared
{
    public class WorkflowsInfoQuery
    {
        public static List<TableEntity> ListAllWorkflows(params string[] select)
        {
            List<string> querySelect = new List<string> { "FlowName", "ChangedTime", "Kind" };
            querySelect.AddRange(select);

            List<TableEntity> entities = TableOperations.QueryMainTable(null, select: querySelect.Distinct().ToArray())
                    .GroupBy(t => t.GetString("FlowName"))
                    .Select(g => g.OrderByDescending(
                        x => x.GetDateTimeOffset("ChangedTime"))
                        .FirstOrDefault())
                    .ToList();

            if (entities.Count == 0)
            {
                throw new UserInputException("No workflows found.");
            }

            return entities;
        }

        public static List<TableEntity> ListCurrentWorkflows(params string[] select)
        {
            List<string> querySelect = new List<string> { "FlowName", "ChangedTime", "Kind" };

            querySelect.AddRange(select);
            List<TableEntity> entities = TableOperations.QuerySubscriptionSummaryTable($"FlowName ne 'null'", select: querySelect.Distinct().ToArray());

            if (entities.Count == 0)
            {
                throw new UserInputException("No workflows found.");
            }

            return entities;
        }

        public static List<TableEntity> ListWorkflowsByName(string workflowName, params string[] select)
        {
            List<string> querySelect = new List<string> { "FlowId", "ChangedTime", "Kind" };
            querySelect.AddRange(select);

            List<TableEntity> entities = TableOperations.QueryMainTable($"FlowName eq '{workflowName}'", select: querySelect.Distinct().ToArray())
                            .GroupBy(t => t.GetString("FlowId"))
                            .Select(g => g.OrderByDescending(x => x.GetDateTimeOffset("ChangedTime"))
                                    .FirstOrDefault())
                            .ToList();

            if (entities.Count == 0)
            {
                throw new UserInputException("No workflows found.");
            }

            return entities;
        }

        public static List<TableEntity> ListVersionsByID(string flowID, params string[] select)
        {
            List<string> querySelect = new List<string> { "RowKey", "ChangedTime", "FlowSequenceId" };
            querySelect.AddRange(select);

            List<TableEntity> entities = TableOperations.QueryMainTable($"FlowId eq '{flowID}'", select: querySelect.Distinct().ToArray())
                            .Where(t => t.GetString("RowKey").StartsWith("MYEDGEENVIRONMENT_FLOWVERSION"))
                            .OrderByDescending(t => t.GetDateTimeOffset("ChangedTime"))
                            .ToList();

            if (entities.Count == 0)
            {
                throw new UserInputException("No workflows found.");
            }

            return entities;
        }

        public static string QueryWorkflowID(string workflowName)
        {
            List<TableEntity> tableEntities = TableOperations.QueryCurrentWorkflowByName(workflowName, new string[] { "FlowId" });

            if (tableEntities.Count() == 0)
            {
                throw new UserInputException($"No existing workflow named {workflowName}, please review your input.");
            }

            return tableEntities.First<TableEntity>().GetString("FlowId");
        }

        public static string QueryDefinitionByFlowName(string workflowName)
        {
            List<TableEntity> tableEntities = TableOperations.QueryCurrentWorkflowByName(workflowName, new string[] { "DefinitionCompressed" });

            if (tableEntities.Count() == 0)
            {
                throw new UserInputException($"No existing workflow named {workflowName}, please review your input.");
            }

            byte[] definitionCompressed = tableEntities.First<TableEntity>().GetBinary("DefinitionCompressed");
            return CommonOperations.DecompressContent(definitionCompressed);
        }
    }
}
