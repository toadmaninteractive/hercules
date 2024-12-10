using Hercules.Documents;
using Hercules.Forms;
using Hercules.Forms.Schema;
using Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;

namespace Hercules.Summary
{
    public class TableColumn : IExportColumn
    {
        public SummaryTable Table { get; }
        public JsonPath Path { get; }
        public SchemaType Type { get; }
        public string Name { get; }
        public string PropertyName { get; }
        public string CellPropertyName { get; }
        public bool IsId { get; }

        public bool IsReadOnly => IsId;

        public Type CellType { get; }

        public TableColumn(SummaryTable table, SchemaType type, JsonPath path)
        {
            this.Table = table;
            this.Type = type;
            this.Path = path;
            this.Name = path.ToString();
            this.PropertyName = Name.Replace(".", "__", StringComparison.Ordinal).Replace("[", "__", StringComparison.Ordinal).Replace("]", "__", StringComparison.Ordinal);
            this.CellPropertyName = "Cell___" + PropertyName;
            this.IsId = this.Name == "_id";
            this.CellType = GetCellType(Type.Kind);
        }

        private Type GetCellType(SchemaTypeKind kind)
        {
            if (IsId)
                return typeof(DocumentIdTableCell);

            switch (kind)
            {
                case SchemaTypeKind.Int:
                    return typeof(IntTableCell);

                case SchemaTypeKind.Float:
                    return typeof(FloatTableCell);

                case SchemaTypeKind.String:
                case SchemaTypeKind.Text:
                case SchemaTypeKind.Path:
                    return typeof(StringTableCell);

                case SchemaTypeKind.Enum:
                    return typeof(EnumTableCell);

                case SchemaTypeKind.SelectString:
                    return typeof(SelectTableCell);

                case SchemaTypeKind.Key:
                    return typeof(KeyTableCell);

                case SchemaTypeKind.Bool:
                    return typeof(BoolTableCell);

                case SchemaTypeKind.DateTime:
                    return typeof(DateTimeTableCell);

                case SchemaTypeKind.Json:
                    return typeof(JsonTableCell);

                case SchemaTypeKind.MultiSelect:
                    return typeof(MultiSelectTableCell);

                default:
                    throw new ArgumentException("Invalid cell type: " + kind);
            }
        }

        public TableCell CreateCell(TableRow row)
        {
            if (IsId)
                return new DocumentIdTableCell(this, row);

            if (row.Document == null || !row.Document.Json.TryFetch(Path, out var data))
                data = null;

            return Type.Kind switch
            {
                SchemaTypeKind.Int => new IntTableCell(this, row, data),
                SchemaTypeKind.Float => new FloatTableCell(this, row, data),
                SchemaTypeKind.String => new StringTableCell(this, row, data),
                SchemaTypeKind.Text => new StringTableCell(this, row, data),
                SchemaTypeKind.Path => new StringTableCell(this, row, data),
                SchemaTypeKind.Enum => new EnumTableCell(this, row, data),
                SchemaTypeKind.SelectString => new SelectTableCell(this, row, data),
                SchemaTypeKind.Key => new KeyTableCell(this, row, data),
                SchemaTypeKind.Bool => new BoolTableCell(this, row, data),
                SchemaTypeKind.DateTime => new DateTimeTableCell(this, row, data),
                SchemaTypeKind.Json => new JsonTableCell(this, row, data),
                SchemaTypeKind.MultiSelect => new MultiSelectTableCell(this, row, data),
                _ => throw new ArgumentException("Invalid cell type: " + Type.Kind)
            };
        }
    }

    public class TableRow : DynamicObject, INotifyPropertyChanged, IComparable<TableRow>
    {
        private readonly Dictionary<string, TableCell> cells = new();

        public bool IsModified { get; private set; }
        public string DocumentId { get; }
        public IDocument? Document { get; }
        public SummaryTable Table { get; }

        public TableRow(SummaryTable table, IDocument document)
        {
            this.Table = table;
            this.Document = document;
            this.DocumentId = document.DocumentId;

            foreach (var column in table.Columns)
                AddColumn(column);
        }

        public TableRow(SummaryTable table, string documentId)
        {
            this.Table = table;
            this.DocumentId = documentId;
            this.IsModified = true;

            foreach (var column in table.Columns)
                AddColumn(column);
        }

        public void AddColumn(TableColumn column)
        {
            var cell = column.CreateCell(this);
            cells.Add(column.CellPropertyName, cell);
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return cells.Keys;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            result = this[binder.Name];
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object? value)
        {
            this[binder.Name] = value;
            return true;
        }

        public object? this[string propertyName]
        {
            get
            {
                if (propertyName.StartsWith("Cell___", StringComparison.Ordinal))
                    return cells[propertyName];
                else
                    return cells["Cell___" + propertyName].ObjectValue;
            }
            set
            {
            }
        }

        public TableCell GetCell(TableColumn column)
        {
            return cells[column.CellPropertyName];
        }

        public void Modified(TableCell cell)
        {
            OnPropertyChanged(cell.Column.PropertyName);
            IsModified = true;
            Table.IsModified = true;
        }

        void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion INotifyPropertyChanged Members

        public int CompareTo(TableRow? other)
        {
            if (other is null)
                return 1;
            return string.Compare(DocumentId, other.DocumentId, StringComparison.Ordinal);
        }
    }

    public class SummaryTable : NotifyPropertyChanged, IExportTable
    {
        bool isModified;

        public bool IsModified
        {
            get => isModified;
            set => SetField(ref isModified, value);
        }

        public ICommand<IDocument> EditDocumentCommand { get; }

        public FormSettings FormSettings { get; }

        public SummaryTable(SchemaRecord schemaRecord, FormSettings formSettings, ICommand<IDocument> editDocumentCommand, IEnumerable<JsonPath> fields)
        {
            FormSettings = formSettings;
            EditDocumentCommand = editDocumentCommand;
            Rows = new ObservableCollection<TableRow>();
            Columns = new ObservableCollection<TableColumn>
            {
                new TableColumn(this, new StringSchemaType(), new JsonPath("_id"))
            };
            this.schemaRecord = schemaRecord;
            foreach (var path in fields)
            {
                var type = schemaRecord.GetByPath(path);
                if (type != null)
                    Columns.Add(new TableColumn(this, type, path));
            }
        }

        public TableColumn AddColumn(JsonPath path)
        {
            var type = schemaRecord.GetByPath(path);
            if (type == null)
                throw new ArgumentException($"Column {path} not found", nameof(path));
            var column = new TableColumn(this, type, path);
            Columns.Add(column);
            foreach (var row in Rows)
            {
                row.AddColumn(column);
            }
            return column;
        }

        public TableRow AddRow(IDocument document)
        {
            var row = new TableRow(this, document);
            Rows.InsertSorted(row);
            return row;
        }

        public TableRow AddNewRow(string documentId)
        {
            var row = new TableRow(this, documentId);
            Rows.InsertSorted(row);
            return row;
        }

        private readonly SchemaRecord schemaRecord;
        public ObservableCollection<TableRow> Rows { get; }
        public ObservableCollection<TableColumn> Columns { get; }

        public int RowCount => Rows.Count;
        IReadOnlyList<IExportColumn> IExportTable.ExportColumns => Columns;
        public object? GetExportValue(int rowIndex, IExportColumn column)
        {
            return Rows[rowIndex].GetCell((TableColumn)column).ObjectValue;
        }
    }
}
