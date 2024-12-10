using System.Collections.Generic;
using System.Linq;

namespace Hercules.Import
{
    public class RawTableValue
    {
        public string? Value { get; set; }

        public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

        public RawTableValue(string? value)
        {
            Value = value;
        }
    }

    public class RawTableRow
    {
        public RawTableRow(IEnumerable<string?> items)
        {
            this.Items = items.Select(s => new RawTableValue(s)).ToList();
        }

        public List<RawTableValue> Items { get; private set; }

        public bool IsEmpty => Items.All(i => i.IsEmpty);
    }

    public class RawTable : NotifyPropertyChanged
    {
        public RawTable()
        {
            Rows = new List<RawTableRow>();
        }

        public void AddRow(IEnumerable<string?> row)
        {
            var myRow = new RawTableRow(row);
            if (myRow.Items.Count > ColumnCount)
                ColumnCount = myRow.Items.Count;
            Rows.Add(myRow);
        }

        public List<RawTableRow> Rows { get; private set; }
        public int ColumnCount { get; private set; }

        public void RemoveEmptyRows()
        {
            Rows.RemoveAll(row => row.IsEmpty);
        }
    }
}
