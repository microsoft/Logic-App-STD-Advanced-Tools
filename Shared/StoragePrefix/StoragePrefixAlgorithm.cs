using System;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace LogicAppAdvancedTool
{
    //method for generating the storage table name prefix
    //In Logic App Standard, we need to map the Logic App Name to the storage table name (LAName -> flowxxxxxflows)
    //DO NOT change anything in MurmurHash64 method
    public static partial class StoragePrefixGenerator
	{
        private static string TrimStorageKeyPrefix(string storageKeyPrefix, int limit)
        {
            if (limit < 17)
            {
                throw new ArgumentException(string.Format("The storage key limit should be at least {0} characters.", 17), "limit");
            }
            if (storageKeyPrefix.Length <= limit - 17)
            {
                return storageKeyPrefix;
            }
            return storageKeyPrefix.Substring(0, limit - 17);
        }

        #region Hash Algorithm for LA table
        private static uint MurmurHash32(byte[] data, uint seed = 0U)
        {
            int num = data.Length;
            uint num2 = seed;
            int num3 = 0;
            while (num3 + 3 < num)
            {
                uint num4 = (uint)((int)data[num3] | (int)data[num3 + 1] << 8 | (int)data[num3 + 2] << 16 | (int)data[num3 + 3] << 24);
                num4 *= 3432918353U;
                num4 = num4.RotateLeft32(15);
                num4 *= 461845907U;
                num2 ^= num4;
                num2 = num2.RotateLeft32(13);
                num2 = num2 * 5U + 3864292196U;
                num3 += 4;
            }
            int num5 = num - num3;
            if (num5 > 0)
            {
                uint num6 = (uint)((num5 == 3) ? ((int)data[num3] | (int)data[num3 + 1] << 8 | (int)data[num3 + 2] << 16) : ((num5 == 2) ? ((int)data[num3] | (int)data[num3 + 1] << 8) : ((int)data[num3])));
                num6 *= 3432918353U;
                num6 = num6.RotateLeft32(15);
                num6 *= 461845907U;
                num2 ^= num6;
            }
            num2 ^= (uint)num;
            num2 ^= num2 >> 16;
            num2 *= 2246822507U;
            num2 ^= num2 >> 13;
            num2 *= 3266489909U;
            return num2 ^ num2 >> 16;
        }

        private static ulong MurmurHash64(byte[] data, uint seed = 0U)
		{
			int num = data.Length;
			uint num2 = seed;
			uint num3 = seed;
			int num4 = 0;
			while (num4 + 7 < num)
			{
				uint num5 = (uint)((int)data[num4] | (int)data[num4 + 1] << 8 | (int)data[num4 + 2] << 16 | (int)data[num4 + 3] << 24);
				uint num6 = (uint)((int)data[num4 + 4] | (int)data[num4 + 5] << 8 | (int)data[num4 + 6] << 16 | (int)data[num4 + 7] << 24);
				num5 *= 597399067U;
				num5 = RotateLeft32(num5, 15);
				num5 *= 2869860233U;
				num2 ^= num5;
				num2 = RotateLeft32(num2, 19);
				num2 += num3;
				num2 = num2 * 5U + 1444728091U;
				num6 *= 2869860233U;
				num6 = RotateLeft32(num6, 17);
				num6 *= 597399067U;
				num3 ^= num6;
				num3 = RotateLeft32(num3, 13);
				num3 += num2;
				num3 = num3 * 5U + 197830471U;
				num4 += 8;
			}
			int num7 = num - num4;
			if (num7 > 0)
			{
				uint num8 = (uint)((num7 >= 4) ? ((int)data[num4] | (int)data[num4 + 1] << 8 | (int)data[num4 + 2] << 16 | (int)data[num4 + 3] << 24) : ((num7 == 3) ? ((int)data[num4] | (int)data[num4 + 1] << 8 | (int)data[num4 + 2] << 16) : ((num7 == 2) ? ((int)data[num4] | (int)data[num4 + 1] << 8) : ((int)data[num4]))));
				num8 *= 597399067U;
				num8 = RotateLeft32(num8, 15);
				num8 *= 2869860233U;
				num2 ^= num8;
				if (num7 > 4)
				{
					uint num9 = (uint)((num7 == 7) ? ((int)data[num4 + 4] | (int)data[num4 + 5] << 8 | (int)data[num4 + 6] << 16) : ((num7 == 6) ? ((int)data[num4 + 4] | (int)data[num4 + 5] << 8) : ((int)data[num4 + 4])));
					num9 *= 2869860233U;
					num9 = RotateLeft32(num9, 17);
					num9 *= 597399067U;
					num3 ^= num9;
				}
			}
			num2 ^= (uint)num;
			num3 ^= (uint)num;
			num2 += num3;
			num3 += num2;
			num2 ^= num2 >> 16;
			num2 *= 2246822507U;
			num2 ^= num2 >> 13;
			num2 *= 3266489909U;
			num2 ^= num2 >> 16;
			num3 ^= num3 >> 16;
			num3 *= 2246822507U;
			num3 ^= num3 >> 13;
			num3 *= 3266489909U;
			num3 ^= num3 >> 16;
			num2 += num3;
			num3 += num2;
			return (ulong)num3 << 32 | (ulong)num2;
		}

		private static uint RotateLeft32(this uint value, int count)
		{
			return value << count | value >> 32 - count;
		}
		#endregion
	}
}
