using Hercules.Documents;
using Hercules.Forms.Schema;
using Hercules.Shortcuts;
using Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hercules.Summary
{
    public abstract class TableCell : NotifyPropertyChanged
    {
        protected TableCell(TableColumn column, TableRow row)
        {
            this.Column = column;
            this.Row = row;
        }

        public TableColumn Column { get; }
        public TableRow Row { get; }

        bool isModified;

        public bool IsModified
        {
            get => isModified;
            set => SetField(ref isModified, value);
        }

        public abstract object? ObjectValue { get; set; }

        public abstract string StringValue { get; set; }

        public abstract void Clear();

        protected virtual void UpdateValue()
        {
            RaisePropertyChanged(nameof(StringValue));
            IsModified = true;
            Row.Modified(this);
        }

        public abstract ImmutableJson? Json { get; }
    }

    public class DocumentIdTableCell : TableCell
    {
        public DocumentIdTableCell(TableColumn column, TableRow row) : base(column, row)
        {
        }

        public override object? ObjectValue
        {
            get => Row.DocumentId;
            set => throw new NotSupportedException();
        }

        public override string StringValue
        {
            get => Row.DocumentId;
            set => throw new NotSupportedException();
        }

        public string Value => StringValue;

        public override void Clear()
        {
        }

        public override ImmutableJson? Json => ImmutableJson.Create(Row.DocumentId);
    }

    public class SimpleTableCell<T> : TableCell where T : class
    {
        T? val;

        public T? Value
        {
            get => val;
            set
            {
                var translatedValue = Type.TranslateValue(value);
                if (!Type.ValueEquals(val, translatedValue))
                {
                    val = translatedValue;
                    RaisePropertyChanged();
                    UpdateValue();
                }
            }
        }

        public SimpleSchemaType<T?> Type => (SimpleSchemaType<T?>)Column.Type;

        public override object? ObjectValue
        {
            get => val;
            set
            {
                if (value == null)
                    Value = null;
                else if (value is T)
                    Value = (T)value;
                else if (value is string)
                    StringValue = (string)value;
                else
                    Value = null;
            }
        }

        public override string StringValue
        {
            get => val == null ? string.Empty : Type.ConvertToString(val);
            set
            {
                if (!string.IsNullOrEmpty(value) && Type.ConvertFromString(value, out var import))
                    Value = import;
                else
                    Value = null;
            }
        }

        public override void Clear()
        {
            Value = null;
        }

        public SimpleTableCell(TableColumn column, TableRow row, ImmutableJson? data)
            : base(column, row)
        {
            if (!Type.ConvertFromJson(data, out val))
                val = null;
        }

        public override ImmutableJson? Json => Value == null ? null : Type.ConvertToJson(Value);
    }

    public class NullableTableCell<T> : TableCell where T : struct
    {
        T? val;

        public T? Value
        {
            get => val;
            set
            {
                if (!ValueEquals(val, value))
                {
                    val = value;
                    RaisePropertyChanged();
                    UpdateValue();
                }
            }
        }

        bool ValueEquals(T? v1, T? v2)
        {
            if (v1.HasValue && v2.HasValue)
                return Type.ValueEquals(v1.Value, v2.Value);
            else
                return v1.HasValue == v2.HasValue;
        }

        public SimpleSchemaType<T> Type => (SimpleSchemaType<T>)Column.Type;

        public override object? ObjectValue
        {
            get => val;
            set
            {
                if (value == null)
                    Value = null;
                else if (value is T)
                    Value = (T)value;
                else if (value is string)
                    StringValue = (string)value;
                else
                    Value = null;
            }
        }

        public override string StringValue
        {
            get => val.HasValue ? Type.ConvertToString(val.Value) : string.Empty;
            set
            {
                if (Type.ConvertFromString(value, out var import))
                    Value = import;
                else
                    Value = null;
            }
        }

        public override void Clear()
        {
            Value = null;
        }

        public override ImmutableJson? Json => Value.HasValue ? Type.ConvertToJson(Value.Value) : null;

        public NullableTableCell(TableColumn column, TableRow row, ImmutableJson? data)
            : base(column, row)
        {
            if (Type.ConvertFromJson(data, out var bVal))
                val = bVal;
            else
                val = null;
        }
    }

    public class IntTableCell : NullableTableCell<int>
    {
        public IntTableCell(TableColumn column, TableRow row, ImmutableJson? data)
            : base(column, row, data)
        {
        }
    }

    public class FloatTableCell : NullableTableCell<double>
    {
        public FloatTableCell(TableColumn column, TableRow row, ImmutableJson? data)
            : base(column, row, data)
        {
        }
    }

    public class StringTableCell : SimpleTableCell<string>
    {
        public StringTableCell(TableColumn column, TableRow row, ImmutableJson? data)
            : base(column, row, data)
        {
        }
    }

    public class EnumTableCell : SimpleTableCell<string>
    {
        public EnumTableCell(TableColumn column, TableRow row, ImmutableJson? data)
            : base(column, row, data)
        {
        }
    }

    public class KeyTableCell : SimpleTableCell<string>
    {
        public KeyTableCell(TableColumn column, TableRow row, ImmutableJson? data)
            : base(column, row, data)
        {
            UpdateShortcut();
        }

        protected override void UpdateValue()
        {
            base.UpdateValue();
            RaisePropertyChanged(nameof(Document));
            UpdateShortcut();
        }

        IShortcut? shortcut;

        public IShortcut? Shortcut
        {
            get => shortcut;
            set => SetField(ref shortcut, value);
        }

        void UpdateShortcut()
        {
            Shortcut = Value != null ? new DocumentShortcut(Value) : null;
        }

        public IDocument? Document
        {
            get
            {
                if (string.IsNullOrEmpty(Value))
                    return null;
                else
                    return ((KeySchemaType)Type).Items.OfType<IDocument>().FirstOrDefault(doc => doc.DocumentId == Value);
            }
            set
            {
            }
        }
    }

    public class SelectTableCell : SimpleTableCell<string>
    {
        public SelectTableCell(TableColumn column, TableRow row, ImmutableJson? data)
            : base(column, row, data)
        {
        }
    }

    public class BoolTableCell : NullableTableCell<bool>
    {
        public BoolTableCell(TableColumn column, TableRow row, ImmutableJson? data)
            : base(column, row, data)
        {
        }
    }

    public class DateTimeTableCell : NullableTableCell<DateTime>
    {
        public DateTimeTableCell(TableColumn column, TableRow row, ImmutableJson? data)
            : base(column, row, data)
        {
        }
    }

    public class MultiSelectTableCell : SimpleTableCell<IReadOnlyList<string>>
    {
        public MultiSelectTableCell(TableColumn column, TableRow row, ImmutableJson? data) : base(column, row, data)
        {
        }

        public override object? ObjectValue
        {
            get => StringValue;
            set
            {
                if (value == null)
                    Value = null;
                else if (value is IReadOnlyList<string>)
                    Value = (IReadOnlyList<string>)value;
                else if (value is string)
                    StringValue = (string)value;
                else
                    Value = null;
            }
        }
    }

    public class JsonTableCell : TableCell
    {
        string? val;

        public string? Value
        {
            get => val;
            set
            {
                if (!ImmutableJson.Equals(JsonTypes.TryParse(val), JsonTypes.TryParse(value)))
                {
                    val = value;
                    RaisePropertyChanged();
                    UpdateValue();
                }
            }
        }

        public JsonSchemaType Type => (JsonSchemaType)Column.Type;

        public override object? ObjectValue
        {
            get => val;
            set
            {
                if (value == null)
                    Value = null;
                else if (value is string)
                    StringValue = (string)value;
                else
                    Value = null;
            }
        }

        public override string StringValue
        {
            get => val ?? string.Empty;
            set => Value = value;
        }

        public override void Clear()
        {
            Value = null;
        }

        public JsonTableCell(TableColumn column, TableRow row, ImmutableJson? data)
            : base(column, row)
        {
            if (data == null)
                val = null;
            else
                val = data.ToString(JsonFormat.Compact);
        }

        public override ImmutableJson? Json => JsonTypes.TryParse(Value) ?? null;
    }
}