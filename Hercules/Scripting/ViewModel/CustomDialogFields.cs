using Hercules.Forms.Schema;
using Hercules.Scripting.JavaScript;
using Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Telerik.Windows.Controls;

namespace Hercules.Scripting
{
    public abstract class CustomDialogField : NotifyPropertyChanged
    {
        public string Name { get; }
        public string Caption { get; }

        protected CustomDialogField(string name, string caption)
        {
            Name = name;
            Caption = caption;
        }

        public abstract object GetJsValue();

        public static CustomDialogField FromJson(string name, ImmutableJson json)
        {
            static DateTime JsonToDateTime(ImmutableJson? json)
            {
                if (json == null)
                    return DateTime.Now;
                if (json is ImmutableJsonDateTime dt)
                    return dt.Value;
                else if (json.IsString)
                    return DateTime.Parse(json.AsString, DateTimeSchemaType.Culture);
                else
                    return DateTime.Now;
            }

            CustomDialogField FromJsonObject(string name, ImmutableJsonObject obj)
            {
                var type = obj.GetValueOrDefault("type")?.AsString;
                var value = obj.GetValueOrDefault("value");
                var caption = obj.GetValueOrDefault("caption")?.AsString ?? name;
                var values = obj.GetValueOrDefault("values");
                return type switch
                {
                    "bool" => new BoolCustomDialogField(name, caption, value?.AsBool ?? false),
                    "int" => new IntCustomDialogField(name, caption, value?.AsInt ?? 0),
                    "float" => new FloatCustomDialogField(name, caption, value?.AsNumber ?? 0.0),
                    _ when values != null && values.IsArray => new EnumCustomDialogField(name, caption, value?.AsString ?? values.AsArray.First().AsString, values.AsArray.Select(v => v.AsString).ToList()),
                    "string" => new StringCustomDialogField(name, caption, value?.AsString ?? ""),
                    "datetime" => new DateTimeCustomDialogField(name, caption, JsonToDateTime(value), InputMode.DateTimePicker),
                    "date" => new DateTimeCustomDialogField(name, caption, JsonToDateTime(value), InputMode.DatePicker),
                    "time" => new DateTimeCustomDialogField(name, caption, JsonToDateTime(value), InputMode.TimePicker),
                    null => new StringCustomDialogField(name, caption, value?.AsString ?? ""),
                    string other => throw new InvalidOperationException($"Unknown custom dialog field type: {other.ToString(JsonFormat.Compact)}")
                };
            }

            return json switch
            {
                ImmutableJsonBoolean { Value: var value } => new BoolCustomDialogField(name, name, value),
                ImmutableJsonInteger { Value: var value } => new IntCustomDialogField(name, name, value),
                ImmutableJsonLong { Value: var value } => new IntCustomDialogField(name, name, (int)value),
                ImmutableJsonDouble { Value: var value } => new FloatCustomDialogField(name, name, value),
                ImmutableJsonString { Value: var value } => new StringCustomDialogField(name, name, value),
                ImmutableJsonArray { AsArray: var array } => new EnumCustomDialogField(name, name, array.First().AsString, array.Select(v => v.AsString).ToList()),
                ImmutableJsonDateTime { Value: var dateTime } => new DateTimeCustomDialogField(name, name, dateTime, InputMode.DateTimePicker),
                ImmutableJsonObject { AsObject: var obj } => FromJsonObject(name, obj),
                ImmutableJson other => throw new InvalidOperationException($"Unknown custom dialog field type: {other.ToString(JsonFormat.Compact)}"),
            };
        }
    }

    public abstract class CustomDialogField<T> : CustomDialogField
    {
        private T value;
        public T Value
        {
            get => value;
            set => SetField(ref this.value, value);
        }

        protected CustomDialogField(string name, string caption, T value) : base(name, caption)
        {
            this.value = value;
        }
    }

    public class BoolCustomDialogField : CustomDialogField<bool>
    {
        public BoolCustomDialogField(string name, string caption, bool value) : base(name, caption, value)
        {
        }

        public override object GetJsValue() => Value;
    }

    public class IntCustomDialogField : CustomDialogField<int>
    {
        public IntCustomDialogField(string name, string caption, int value) : base(name, caption, value)
        {
        }

        public override object GetJsValue() => Value;
    }

    public class FloatCustomDialogField : CustomDialogField<double>
    {
        public FloatCustomDialogField(string name, string caption, double value) : base(name, caption, value)
        {
        }

        public override object GetJsValue() => Value;
    }

    public class StringCustomDialogField : CustomDialogField<string>
    {
        public StringCustomDialogField(string name, string caption, string value) : base(name, caption, value)
        {
        }

        public override object GetJsValue() => Value;
    }

    public class EnumCustomDialogField : CustomDialogField<string>
    {
        public EnumCustomDialogField(string name, string caption, string value, IReadOnlyList<string> values) : base(name, caption, value)
        {
            Values = values;
        }

        public IReadOnlyList<string> Values { get; }

        public override object GetJsValue() => Value;
    }

    public class DateTimeCustomDialogField : CustomDialogField<DateTime>
    {
        public DateTimeCustomDialogField(string name, string caption, DateTime value, InputMode inputMode) : base(name, caption, value)
        {
            InputMode = inputMode;
        }

        public override object GetJsValue() => Value;

        public InputMode InputMode { get; }
    }
}
