using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicAppAdvancedTool
{
    public partial class Program
    {
        public class ConsoleTable
        {
            private int ColumnCount;
            private List<string[]> Rows;
            private List<int> ColumnWidth;

            public ConsoleTable(params string[] headers) 
            {
                ColumnCount = headers.Length;

                Rows = new List<string[]>();
                Rows.Add(headers);

                ColumnWidth = new List<int>();
                foreach (string header in headers)
                {
                    ColumnWidth.Add(header.Length);
                }
            }

            public void AddRow(params string[] contents)
            {
                if (contents.Length != ColumnCount)
                {
                    throw new Exception("Column count mismatch");
                }

                Rows.Add(contents);

                for (int i = 0; i < contents.Length; i++)
                {
                    if (ColumnWidth[i] < contents[i].Length - 2) 
                    {
                        ColumnWidth[i] = contents[i].Length + 2;
                    }
                }
            }

            public void Print()
            {
                int RowLength = 0;

                foreach (int l in ColumnWidth)
                { 
                    RowLength+= l;
                }

                RowLength += ColumnWidth.Count + 1;

                Console.WriteLine(new string('-', RowLength));

                foreach (string[] row in Rows)
                {
                    StringBuilder rowContent = new StringBuilder();

                    for (int i = 0; i < row.Length; i++)
                    {
                        rowContent.Append("|");
                        rowContent.Append(string.Format($"{row[i]}".PadRight(ColumnWidth[i])));
                    }

                    rowContent.Append("|");

                    Console.WriteLine(rowContent.ToString());
                    Console.WriteLine(new string('-', RowLength));
                }
            }
        }
    }
}
