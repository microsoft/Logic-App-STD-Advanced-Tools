using LogicAppAdvancedTool.Structures;
using System;
using System.IO;
using System.Net.Http;
using Microsoft.DiaSymReader;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Azure.Storage.Blobs;
using LogicAppAdvancedTool.Operations;
using Microsoft.WindowsAzure.ResourceStack.Common.Utilities;

namespace LogicAppAdvancedTool
{
    internal partial class Tools
    {
        public static void FeatureTesting()
        {
            Decode.Run("Test", "08584702998773857900");
        }
    }
}
