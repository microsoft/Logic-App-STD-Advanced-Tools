﻿using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace LogicAppAdvancedTool
{
    //method for generating the storage table name prefix
    //In Logic App Standard, we need to map the Logic App Name to the storage table name (LAName -> flowxxxxxflows)
    //DO NOT change anything in MurmurHash64 method
    public static partial class StoragePrefixGenerator
	{
		private static string Generate(string name)
		{
			byte[] data = Encoding.UTF8.GetBytes(name.ToLower());

			string hashResult = MurmurHash64(data, 0U).ToString("X");

			return TrimStorageKeyPrefix(hashResult, 32).ToLower();
		}

        //When Logic App environment variable has "AzureFunctionsWebHost__hostId", the prefix will be generated based on the hostId
        public static string GenerateLogicAppPrefix()
		{
            string HostID = AppSettings.HostID;

            return Generate(HostID.ToLower());
        }

		public static string GenerateWorkflowPrefix(string workflowID)
        {
            return Generate(workflowID);
        }

        public static string GeneratePartitionKey(string rowKey)
		{
			string key = rowKey.Split('_')[0];

			byte[] data = Encoding.UTF8.GetBytes(key);

			uint result = MurmurHash32(data, 0U) % 1048576;

			return result.ToString("X5");
		}

        public static string GenerateWorkflowTablePrefixByFlowID(string workflowID)
        {
            string logicAppPrefix = StoragePrefixGenerator.GenerateLogicAppPrefix();

            string workflowPrefix = StoragePrefixGenerator.GenerateWorkflowPrefix(workflowID);

            return $"{logicAppPrefix}{workflowPrefix}";
        }

        public static string GenerateWorkflowTablePrefixByName(string workflowName)
        {
            List<TableEntity> tableEntities = TableOperations.QueryCurrentWorkflowByName(workflowName, new string[] { "FlowId" });

            if (tableEntities.Count() == 0)
            {
                throw new UserInputException($"{workflowName} cannot be found in storage table, please check whether workflow name is correct.");
            }

            string workflowID = tableEntities.First<TableEntity>().GetString("FlowId");

            return GenerateWorkflowTablePrefixByFlowID(workflowID);
        }
    }
}
