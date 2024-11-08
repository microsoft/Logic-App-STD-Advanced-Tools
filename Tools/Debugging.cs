using LogicAppAdvancedTool.Structures;
using System;
using System.IO;
using System.Net.Http;
using Microsoft.DiaSymReader;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace LogicAppAdvancedTool
{
    internal partial class Tools
    {
        public static void FeatureTesting()
        {
            RunIDToDatetime("08584737551867954143243946780CU57");
            RunIDToDatetime("08584737550808373605660359857CU62");
            RunIDToDatetime("08584738342725975605125434365CU211");
        }
    }
}
