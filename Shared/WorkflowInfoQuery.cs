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
    }
}
