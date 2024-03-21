using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using System.Collections.Generic;
using System.Linq;

namespace LogicAppAdvancedTool
{
    public class ServiceTagRetriever
    {
        private Dictionary<string, string> ServiceTags;

        public ServiceTagRetriever() 
        { 
            ServiceTags = new Dictionary<string, string>();
        }

        private void LoadServiceTags()
        { 
            
        }
    }
}
