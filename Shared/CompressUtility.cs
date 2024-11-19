using Microsoft.WindowsAzure.ResourceStack.Common.Services;
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
                    memoryStream.Position--;
                    return ZSTDDecompress(memoryStream);
                default:
                    break;
            }

            return null;
        }

        public static byte[] CompressContent(string uncompressedContent)
        {
            return ZSTDCompress(uncompressedContent);
        }

        #region Defalte related methods
        private static string DeflateDecompress(byte[] compressedContent)
        {
            string result = DeflateCompressionUtility.Instance.InflateString(new MemoryStream(compressedContent));

            return result;
        }
        #endregion

        #region ZSTD related methods
        private static byte[] ZSTDCompress(string uncompressedContent)
        {
            byte[] rawBytes = Encoding.UTF8.GetBytes(uncompressedContent);
            MemoryStream resultStream = new MemoryStream();
            using (MemoryStream rawStream = new MemoryStream(rawBytes))
            {
                WriteVariableLengthInteger(resultStream, (long)rawBytes.Length * 8L | (byte)7);
                using (var compressStream = new CompressionStream(resultStream, 1, 0, false))   // 1 is the compression level as fastest
                {
                    rawStream.CopyTo(compressStream);
                }
            }

            return resultStream.ToArray();
        }

        private static void WriteVariableLengthInteger(Stream stream, long value)
        {
            do
            {
                stream.WriteByte((byte)((value & 0x7F) | ((value >= 128) ? 128 : 0)));
                value >>= 7;
            }
            while (value != 0L);
        }

        private static string ZSTDDecompress(MemoryStream compressedStream)
        {
            int uncompressedLength = (int)(ReadVariableLengthInteger(compressedStream) >> 3);

            using (DecompressionStream decompressionStream = new DecompressionStream(compressedStream))
            using (MemoryStream temp = new MemoryStream())
            {
                decompressionStream.CopyTo(temp);

                return Encoding.UTF8.GetString(temp.ToArray());
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