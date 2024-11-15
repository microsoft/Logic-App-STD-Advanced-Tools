using Microsoft.WindowsAzure.ResourceStack.Common.Utilities;
using System;
using System.IO;
using System.Text;
using ZstdSharp;

namespace LogicAppAdvancedTool
{
    public static class CompressUtility
    {
        public static string DecompressContent(byte[] compressedContent)
        {
            MemoryStream memoryStream = new MemoryStream(compressedContent);

            int algorithm = memoryStream.ReadByte() & 7;
            switch (algorithm)
            {
                case 6:
                    throw new Exception("LZ4 compression is not supported");
                case 7:
                    return ZSTDDecompress(memoryStream.ToArray());
                default:
                    break;
            }

            return null;
        }

        private static string DeflateDecompress(byte[] compressedContent)
        {
            string result = DeflateCompressionUtility.Instance.InflateString(new MemoryStream(compressedContent));

            return result;
        }

        #region ZSTD related methods
        private static string ZSTDDecompress(byte[] compressedContent)
        {
            using (MemoryStream input = new MemoryStream(compressedContent))
            {
                int uncompressedLength = (int)(ReadVariableLengthInteger(input) >> 3);

                using (DecompressionStream decompressionStream = new DecompressionStream(input))
                using (MemoryStream temp = new MemoryStream())
                {
                    decompressionStream.CopyTo(temp);
                    return Encoding.UTF8.GetString(temp.ToArray());
                }
            }
        }

        private static long ReadVariableLengthInteger(Stream stream)
        {
            long num = 0L;
            int num2 = 0;
            while (true)
            {
                int num3 = stream.ReadByte();
                num |= (long)(((ulong)num3 & 0x7FuL) << num2);
                if ((long)num3 < 128L)
                {
                    break;
                }

                num2 += 7;
            }

            return num;
        }
        #endregion
    }
}
