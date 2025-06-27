using Hercules.Shell;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;

namespace Hercules.Scripting
{
    public class CustomTable
    {
        public ObservableCollection<ExpandoObject> Rows { get; }

        public string AsCsv(char delimiter = '\t')
        {
            var rows = new List<List<string>>();
            var colCount = Rows[0].Count();
            for (int i = 0; i < Rows.Count; i++)
            {
                var data = new List<string>(colCount);
                foreach (var cell in Rows[i])
                {
                    var value = cell.ToString();
                    if (i == 0)
                        value = value.Replace(U2024, ".");
                    data.Add(value);
                }
                rows.Add(data);
            }
            return CsvUtils.SaveToString(rows, delimiter);
        }

        private CustomTable(ObservableCollection<ExpandoObject> rows)
        {
            Rows = rows;
        }

        // Use this unicode symbol instead of dot to make bindings work
        private const string U2024 = "․";

        public static CustomTable LoadFromCsv(string text)
        {
            var data = CsvUtils.LoadFromString(text);
            var rows = new ObservableCollection<ExpandoObject>();
            if (data.Count > 0)
            {
                var header = data[0].ToList();
                for (int j = 0; j < header.Count; j++)
                {
                    header[j] = header[j].Replace(".", U2024);
                }
                for (int i = 1; i < data.Count; i++)
                {
                    var rowData = data[i];
                    var row = new ExpandoObject();
                    for (int j = 0; j < rowData.Count; j++)
                    {
                        if (header.Count <= j)
                        {
                            header.Add($"col{j}");
                        }
                        row.TryAdd(header[j], rowData[j]);
                    }
                    rows.Add(row);
                }
            }
            return new CustomTable(rows);
        }
    }

    public class TablePage : Page
    {
        public CustomTable Table { get; }

        public TablePage(string title, CustomTable table)
        {
            Title = title;
            Table = table;
        }
    }
}
