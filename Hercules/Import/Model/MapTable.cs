using Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Hercules.Import
{
    public class MapTableColumn : NotifyPropertyChanged
    {
        public string Field { get; }
        public string Title { get; }
        public int Index { get; }

        bool isSelected = true;

        public bool IsSelected
        {
            get => isSelected;
            set => SetField(ref isSelected, value);
        }

        public MapTableColumn(string field, string title, int index)
        {
            Field = field;
            Title = title;
            Index = index;
        }
    }

    public class MapTableValue : NotifyPropertyChanged
    {
        string? text;

        public string? Text
        {
            get => text;
            set => SetField(ref text, value);
        }

        bool isId;

        public bool IsId
        {
            get => isId;
            set => SetField(ref isId, value);
        }

        bool isValid;

        public bool IsValid
        {
            get => isValid;
            set => SetField(ref isValid, value);
        }

        public MapTableValue(string? text)
        {
            this.text = text;
        }
    }

    public class MapTableRow : NotifyPropertyChanged
    {
        public MapTableRow(IEnumerable<MapTableValue> items, int index)
        {
            this.Items = items.ToList();
            this.Index = index;
        }

        public List<MapTableValue> Items { get; }
        public int Index { get; }
    }

    public class MapTable : NotifyPropertyChanged
    {
        public MapTable()
        {
            Rows = new ObservableCollection<MapTableRow>();
            Columns = new ObservableCollection<MapTableColumn>();
        }

        public MapTable(RawTable rawTable)
            : this()
        {
            var first = rawTable.Rows.First();
            for (int i = 0; i < rawTable.ColumnCount; i++)
            {
                if (i < first.Items.Count)
                {
                    var v = first.Items[i];
                    var field = v.Value;
                    if (string.IsNullOrEmpty(field))
                        field = "col" + i;
                    var title = field.Replace("_", "__", StringComparison.Ordinal);
                    Columns.Add(new MapTableColumn(field, title, i));
                }
                else
                {
                    var field = "col" + i;
                    Columns.Add(new MapTableColumn(field, field, i));
                }
            }
            int r = 0;
            foreach (var row in rawTable.Rows.Skip(1))
            {
                var mapRow = new MapTableRow(row.Items.Select(v => new MapTableValue(v.Value)), r);
                AddRow(mapRow);
            }
        }

        public void AddRow(MapTableRow row)
        {
            Rows.Add(row);
        }

        public ObservableCollection<MapTableRow> Rows { get; }
        public ObservableCollection<MapTableColumn> Columns { get; }

        public ImmutableJson ToJson(MapTableRow row)
        {
            var dict = new JsonObject();
            for (int i = 0; i < row.Items.Count; i++)
            {
                var cell = row.Items[i];
                var column = Columns[i];
                var json = string.IsNullOrEmpty(cell.Text) ? ImmutableJson.Null : cell.Text;
                dict[column.Field] = json;
            }
            return dict;
        }

        public ImmutableJson ToJson()
        {
            var rows = new JsonArray(Rows.Count);
            foreach (var row in Rows)
            {
                rows.Add(ToJson(row));
            }
            return rows;
        }
    }
}
