using Azure.Data.Tables;
using Newtonsoft.Json;
using System;
using System.Data;

namespace LogicAppAdvancedTool.Structures
{
    public class WorkflowTemplate
    {
        public object definition { get; set; }
        public string kind { get; set; }
    }

    public enum ActionType
    {
        Foreach,
        If,
        Switch,
        Until,
        Scope,
        Other
    }

    public class WorkflowConnection
    {
        public string ConnectionType;
        public string ConnectionName;

        public WorkflowConnection(string ConnectionType, string ConnectionName)
        {
            this.ConnectionType = ConnectionType;
            this.ConnectionName = ConnectionName;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            WorkflowConnection conn = obj as WorkflowConnection;

            if ((System.Object)conn == null)
            {
                return false;
            }

            return ConnectionType == conn.ConnectionType && ConnectionName == conn.ConnectionName;
        }

        public override int GetHashCode()
        {
            return (ConnectionType + ConnectionName).GetHashCode();
        }
    }
}

