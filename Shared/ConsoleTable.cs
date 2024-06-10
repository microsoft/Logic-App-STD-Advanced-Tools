using System;
using System.Collections.Generic;
using System.Text;

namespace LogicAppAdvancedTool
{
    public class ConsoleTable
    {
        private int ColumnCount;
        private List<List<string>> Rows;
        private List<int> ColumnWidth;
        
        private bool EnableIndex = false;
        private int CurrentIndex = 1;

        public ConsoleTable(List<string> headers, bool enableIndex = false)
        {
            EnableIndex = enableIndex;

            if (EnableIndex)
            {
                headers.Insert(0, "Index");
            }

            ColumnCount = headers.Count;

            Rows = new List<List<string>>
            {
                headers
            };

            ColumnWidth = new List<int>();
            foreach (string header in headers)
            {
                ColumnWidth.Add(header.Length + 2);
            }
        }

        public void AddRow(List<string> contents)
        {
            if (EnableIndex)
            { 
                contents.Insert(0, CurrentIndex.ToString());
            }

            if (contents.Count != ColumnCount)
            {
                throw new Exception("Column count mismatch");
            }

            Rows.Add(contents);

            for (int i = 0; i < contents.Count; i++)
            {
                if (String.IsNullOrEmpty(contents[i]))
                {
                    continue;
                }

                if (ColumnWidth[i] < contents[i].Length + 2)
                {
                    ColumnWidth[i] = contents[i].Length + 2;
                }
            }

            CurrentIndex++;
        }

        public void Print()
        {
            int rowLength = 0;

            foreach (int l in ColumnWidth)
            {
                rowLength += l;
            }

            rowLength += ColumnWidth.Count + 1;

            Console.WriteLine(new string('-', rowLength));

            foreach (List<string> row in Rows)
            {
                StringBuilder rowContent = new StringBuilder();

                for (int i = 0; i < row.Count; i++)
                {
                    rowContent.Append("|");
                    rowContent.Append(string.Format($" {row[i]}".PadRight(ColumnWidth[i])));
                }

                rowContent.Append("|");

                Console.WriteLine(rowContent.ToString());
                Console.WriteLine(new string('-', rowLength));
            }
        }
    }
}
