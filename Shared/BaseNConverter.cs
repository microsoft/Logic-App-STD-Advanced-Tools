using Microsoft.WindowsAzure.ResourceStack.Common.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Text;
using ZstdSharp;

namespace LogicAppAdvancedTool
{
    public static class BaseNConverter
    {
        private static string BaseString = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public static string BaseNToDec(int n, string baseNString)
        {
            char[] reversed = baseNString.ToCharArray();
            Array.Reverse(reversed);

            long result = 0;

            for (int i = 0; i < reversed.Length; i++)
            { 
                result += BaseString.IndexOf(reversed[i]) * (long)Math.Pow(n, i);
            }

            return result.ToString();
        }

        public static string DecToBaseN(int n, long dec)
        {
            StringBuilder result = new StringBuilder();
            while (dec > 0)
            {
                result.Append(BaseString[(int)(dec % n)]);
                dec /= n;
            }
            char[] reversed = result.ToString().ToCharArray();
            Array.Reverse(reversed);
            return new string(reversed);
        }
    }
}