using Azure;
using Azure.Data.Tables;
using System.IO;

namespace LogicAppAdvancedTool
{
    //Testing code for decode Metadata in job definition table
    public static class Metadata
    {
        public static string Decode(string connectionString, string tableName, string rowKey)
        {
            TableClient tableClient = new TableClient(connectionString, tableName);
            Pageable<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: $"RowKey eq '{rowKey}'");

            foreach (TableEntity tableEntity in tableEntities)
            {
                byte[] BinaryMetadata00 = tableEntity.GetBinary("BinaryMetadata00");
                byte[] BinaryMetadata01 = tableEntity.GetBinary("BinaryMetadata01");
                byte[] BinaryMetadata02 = tableEntity.GetBinary("BinaryMetadata02");
                byte[] BinaryMetadata03 = tableEntity.GetBinary("BinaryMetadata03");
                byte[] BinaryMetadata04 = tableEntity.GetBinary("BinaryMetadata04");
                byte[] BinaryMetadata05 = tableEntity.GetBinary("BinaryMetadata05");
                byte[] BinaryMetadata06 = tableEntity.GetBinary("BinaryMetadata06");
                byte[] BinaryMetadata07 = tableEntity.GetBinary("BinaryMetadata07");
                byte[] BinaryMetadata08 = tableEntity.GetBinary("BinaryMetadata08");
                byte[] BinaryMetadata09 = tableEntity.GetBinary("BinaryMetadata09");
                byte[] BinaryMetadata10 = tableEntity.GetBinary("BinaryMetadata10");
                byte[] BinaryMetadata11 = tableEntity.GetBinary("BinaryMetadata11");
                byte[] BinaryMetadata12 = tableEntity.GetBinary("BinaryMetadata12");
                byte[] BinaryMetadata13 = tableEntity.GetBinary("BinaryMetadata13");
                byte[] BinaryMetadata14 = tableEntity.GetBinary("BinaryMetadata14");
                byte[] BinaryMetadata15 = tableEntity.GetBinary("BinaryMetadata15");

                MemoryStream stream = new MemoryStream(capacity: (15 * BinaryMetadata14.Length) + BinaryMetadata15.Length);

                stream.Write(BinaryMetadata00, 0, BinaryMetadata00.Length);
                stream.Write(BinaryMetadata01, 0, BinaryMetadata01.Length);
                stream.Write(BinaryMetadata02, 0, BinaryMetadata02.Length);
                stream.Write(BinaryMetadata03, 0, BinaryMetadata03.Length);
                stream.Write(BinaryMetadata04, 0, BinaryMetadata04.Length);
                stream.Write(BinaryMetadata05, 0, BinaryMetadata05.Length);
                stream.Write(BinaryMetadata06, 0, BinaryMetadata06.Length);
                stream.Write(BinaryMetadata07, 0, BinaryMetadata07.Length);
                stream.Write(BinaryMetadata08, 0, BinaryMetadata08.Length);
                stream.Write(BinaryMetadata09, 0, BinaryMetadata09.Length);
                stream.Write(BinaryMetadata10, 0, BinaryMetadata10.Length);
                stream.Write(BinaryMetadata11, 0, BinaryMetadata11.Length);
                stream.Write(BinaryMetadata12, 0, BinaryMetadata12.Length);
                stream.Write(BinaryMetadata13, 0, BinaryMetadata13.Length);
                stream.Write(BinaryMetadata14, 0, BinaryMetadata14.Length);
                stream.Write(BinaryMetadata15, 0, BinaryMetadata15.Length);

                stream.Position = 0;

                return Program.DecompressContent(stream.ToArray());
            }

            return null;
        }
    }
}
