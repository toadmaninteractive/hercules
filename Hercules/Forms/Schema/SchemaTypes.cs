using Hercules.Documents;
using Hercules.Repository;
using Hercules.Shortcuts;
using Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Hercules.Forms.Schema
{
    public interface IMultiSelectItemSchemaType
    {
        IReadOnlyList<string> MultiSelectItems { get; }
    }

    public class SchemaField
    {
        public string Name { get; }
        public string Caption { get; }
        public SchemaType Type { get; }
        public bool IsTag { get; }
        public bool IsVisibleInPropertyEditor { get; set; }
        public bool IsKey { get; set; }
        public double TextWidth { get; }

        public SchemaField(string name, string caption, SchemaType type, double textWidth, bool isTag)
        {
            this.Name = name;
            this.Caption = caption;
            this.Type = type;
            this.TextWidth = textWidth;
            this.IsTag = isTag;
            this.IsVisibleInPropertyEditor = true;
        }
    }

    public class SchemaEnum : IComparer<string?>
    {
        public string Name { get; }

        public IReadOnlyList<string> Values { get; }

        public SchemaEnum(string name, IReadOnlyList<string> values)
        {
            this.Name = name;
            this.Values = values;
        }

        public int Compare(string? x, string? y)
        {
            if (x == y)
                return 0;
            if (x is null)
                return -1;
            if (y is null)
                return 1;
            bool xFound = false;
            bool yFound = false;
            foreach (var value in Values)
            {
                if (value == x)
                {
                    if (yFound)
                        return -1;
                    xFound = true;
                }

                if (value == y)
                {
                    if (xFound)
                        return 1;
                    yFound = true;
                }
            }

            return string.CompareOrdinal(x, y);
        }
    }

    public abstract class SchemaStruct
    {
        public string Name { get; }

        public List<SchemaField> Fields { get; }

        public SchemaVariant? Parent { get; }

        public IReadOnlyList<string>? Interfaces { get; }

        public IEnumerable<SchemaField> AllFields => FieldsUpTo(null);

        public SchemaField? CaptionField { get; set; }
        public JsonPath? CaptionPath { get; set; }
        public JsonPath? ImagePath { get; set; }
        public SchemaField? EnabledField { get; set; }

        public IEnumerable<SchemaField> FieldsUpTo(SchemaVariant? ancestor)
        {
            if (Parent != null && Parent != ancestor)
                return Parent.FieldsUpTo(ancestor).Concat(Fields);
            else
                return Fields;
        }

        protected SchemaStruct(string name, SchemaVariant? parent, IReadOnlyList<string>? interfaces)
        {
            this.Name = name;
            this.Fields = new List<SchemaField>();
            this.Parent = parent;
            this.Interfaces = interfaces;
        }

        public SchemaType? GetByPath(JsonPath path)
        {
            if (path.Head is not JsonObjectPathNode node)
                return null;
            var field = AllFields.FirstOrDefault(f => f.Name == node.Key);
            return field?.Type.GetByPath(path.Tail);
        }
    }

    public record ColorRecordSchema(SchemaField Red, SchemaField Green, SchemaField Blue, SchemaField? Alpha);

    public class SchemaRecord : SchemaStruct
    {
        public string? TagValue { get; set; }
        public string? Group { get; set; }
        public SchemaBlock? Block { get; set; }
        public bool IsLink { get; set; }
        public ColorRecordSchema? ColorSchema { get; set; }
        public string? AiHint { get; set; }

        public SchemaRecord(string name, SchemaVariant? parent, IReadOnlyList<string>? interfaces, string? tagValue = null)
            : base(name, parent, interfaces)
        {
            this.TagValue = tagValue;
        }

        public static readonly SchemaRecord Default = new SchemaRecord(string.Empty, null, null);
    }

    public class SchemaVariant : SchemaStruct
    {
        public List<SchemaRecord> Children { get; }

        public string Tag { get; }

        public SchemaField TagField => AllFields.First(f => f.IsTag);

        public SchemaRecord? GetChild(string tagValue)
        {
            return Children.FirstOrDefault(child => child.TagValue == tagValue);
        }

        public SchemaRecord? GetChildForJson(ImmutableJsonObject data)
        {
            var tagValue = CouchUtils.GetCategory(data, Tag);
            if (tagValue == null)
                return null;
            else
                return GetChild(tagValue);
        }

        public IEnumerable<SchemaField> AllChildrenFields
        {
            get { return AllFields.Concat(Children.SelectMany(f => f.FieldsUpTo(this))).Distinct(); }
        }

        public bool IsBlock => Children.Any(child => child.Block != null);

        public SchemaVariant(string name, string tag, SchemaVariant? parent, IReadOnlyList<string>? interfaces)
            : base(name, parent, interfaces)
        {
            this.Tag = tag;
            this.Children = new List<SchemaRecord>();
        }
    }

    public enum SchemaTypeKind
    {
        List,
        Dict,
        Record,
        Variant,
        Int,
        Float,
        Bool,
        String,
        Path,
        Text,
        SelectString,
        Binary,
        DateTime,
        Enum,
        Key,
        MultiSelect,
        Localized,
        Json,
        Custom,
    }

    public abstract class SchemaType : IEquatable<SchemaType?>
    {
        public abstract SchemaTypeKind Kind { get; }

        public virtual Type Type => typeof(object);

        public bool Optional { get; }
        public string? Help { get; }

        public virtual bool IsAtomic => ForceAtomic;
        public bool ForceAtomic { get; set; }
        public virtual bool IsComparable => false;

        public string Name => ToString();

        public virtual SchemaType? GetByPath(JsonPath path)
        {
            if (path.Head == null)
                return this;
            else
                return null;
        }

        public virtual ImmutableJson? TranslateShortcut(IShortcut shortcut) => null;

        public abstract bool Equals(SchemaType? other);

        public override bool Equals(object? obj)
        {
            if (obj is SchemaType type)
                return this.Equals(type);
            else
                return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Kind, Optional, Help);
        }

        protected SchemaType(bool optional, string? help)
        {
            this.Optional = optional;
            this.Help = help;
        }

        public override string ToString() => Kind.ToString().ToLowerInvariant();

        public ImmutableJson ConstrainJson(ImmutableJson? json)
        {
            if (Optional && (json == null || json.IsNull))
                return ImmutableJson.Null;
            return ConstrainJsonNonOptional(json);
        }

        protected abstract ImmutableJson ConstrainJsonNonOptional(ImmutableJson? json);

        public ImmutableJson ConstrainedRebase(ImmutableJson json, ImmutableJson oldBase, ImmutableJson newBase)
        {
            if (IsAtomic)
                return ImmutableJson.Equals(json, oldBase) ? newBase : json;
            if (Optional)
            {
                if (json.IsNull && oldBase.IsNull)
                    return newBase;
                if (json.IsNull != oldBase.IsNull)
                    return json;
            }
            return DeepRebase(json, oldBase, newBase);
        }

        protected virtual ImmutableJson DeepRebase(ImmutableJson json, ImmutableJson oldBase, ImmutableJson newBase) => ImmutableJson.Equals(json, oldBase) ? newBase : json;
    }

    public class ListSchemaType : SchemaType
    {
        public SchemaType ItemType { get; }

        public ShortcutService ShortcutService { get; }

        public ImmutableJsonArray? Default { get; }

        public ListSchemaType(SchemaType itemType, ShortcutService shortcutService, bool optional = false, string? help = null, ImmutableJsonArray? @default = null)
            : base(optional, help)
        {
            this.ItemType = itemType;
            this.ShortcutService = shortcutService;
            this.Default = @default;
        }

        public override SchemaTypeKind Kind => SchemaTypeKind.List;

        public override SchemaType? GetByPath(JsonPath path)
        {
            if (path.Head == null)
                return this;
            else if (path.Head is JsonArrayPathNode)
                return ItemType.GetByPath(path.Tail);
            else
                return null;
        }

        public override bool Equals(SchemaType? other)
        {
            if (other is ListSchemaType that)
                return this.Kind == that.Kind && this.Optional == that.Optional && this.Help == that.Help && this.ItemType.Equals(that.ItemType);
            else
                return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), ItemType);
        }

        protected override ImmutableJson ConstrainJsonNonOptional(ImmutableJson? json)
        {
            if (json == null || !json.IsArray)
                return ImmutableJson.EmptyArray;
            return new JsonArray(json.AsArray.Select(item => ItemType.ConstrainJson(item)));
        }

        protected override ImmutableJson DeepRebase(ImmutableJson json, ImmutableJson oldBase, ImmutableJson newBase)
        {
            if (ItemType.IsAtomic)
                return SmartRebase(json.AsArray, oldBase.AsArray, newBase.AsArray, EqualityComparer<ImmutableJson>.Default);
            if (ItemType is RecordSchemaType record)
            {
                var keyFields = record.AllFields.Where(f => f.IsKey).Select(f => f.Name).ToList();
                if (keyFields.Any())
                {
                    var equalityComparer = new JsonPartialEqualityComparer(keyFields);
                    return SmartRebase(json.AsArray, oldBase.AsArray, newBase.AsArray, equalityComparer);
                }
            }
            return OrderedRebase(json.AsArray, oldBase.AsArray, newBase.AsArray);
        }

        private ImmutableJson SmartRebase(ImmutableJsonArray json, ImmutableJsonArray oldBase, ImmutableJsonArray newBase, IEqualityComparer<ImmutableJson> equalityComparer)
        {
            var rebased = ListRebaser.Rebase(json, oldBase, newBase, equalityComparer, ItemType.ConstrainedRebase);
            return new JsonArray(rebased);
        }

        private ImmutableJson OrderedRebase(ImmutableJsonArray json, ImmutableJsonArray oldBase, ImmutableJsonArray newBase)
        {
            var count = Math.Max(Math.Max(json.Count, oldBase.Count), newBase.Count);
            var result = new JsonArray(count);
            for (int i = 0; i < count; i++)
            {
                var jsonItem = SafeJson.GetItem(json, i);
                var oldBaseItem = SafeJson.GetItem(oldBase, i);
                var newBaseItem = SafeJson.GetItem(newBase, i);
                if (jsonItem != null && oldBaseItem != null && newBaseItem != null)
                {
                    result.Add(ItemType.ConstrainedRebase(jsonItem, oldBaseItem, newBaseItem));
                }
                else if (jsonItem != null && oldBaseItem == null)
                {
                    result.Add(jsonItem);
                }
                else if (jsonItem == null && oldBaseItem == null && newBaseItem != null)
                {
                    result.Add(newBaseItem);
                }
            }
            return result;
        }
    }

    public class DictSchemaType : SchemaType
    {
        public SchemaType KeyType { get; }
        public SchemaType ValueType { get; }
        public IReadOnlyDictionary<string, SchemaType>? ValueTypesPerKey { get; }

        public bool Compact { get; init; }

        public DictSchemaType(SchemaType keyType, SchemaType valueType, IReadOnlyDictionary<string, SchemaType>? valueTypesPerKey = null, bool optional = false, string? help = null, ImmutableJsonObject? @default = null)
            : base(optional, help)
        {
            this.KeyType = keyType;
            this.ValueType = valueType;
            this.ValueTypesPerKey = valueTypesPerKey;
            this.Default = @default;
        }

        public ImmutableJsonObject? Default { get; }

        public override SchemaTypeKind Kind => SchemaTypeKind.Dict;

        public SchemaType ValueTypePerKey(string key)
        {
            if (ValueTypesPerKey == null)
                return ValueType;
            else
                return ValueTypesPerKey.GetValueOrDefault(key, ValueType);
        }

        public override SchemaType? GetByPath(JsonPath path)
        {
            if (path.Head == null)
                return this;
            else if (path.Head is JsonObjectKeyPathNode)
                return KeyType.GetByPath(path.Tail);
            else if (path.Head is JsonObjectPathNode node)
                return ValueTypePerKey(node.Key).GetByPath(path.Tail);
            else
                return null;
        }

        public override bool Equals(SchemaType? other)
        {
            if (other is DictSchemaType that)
                return this.Kind == that.Kind && this.Optional == that.Optional && this.Help == that.Help && this.KeyType.Equals(that.KeyType) && this.ValueType.Equals(that.ValueType);
            else
                return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), KeyType, ValueType);
        }

        protected override ImmutableJson ConstrainJsonNonOptional(ImmutableJson? json)
        {
            if (json == null || !json.IsObject)
                return ImmutableJson.EmptyObject;
            return new JsonObject((IDictionary<string, ImmutableJson>)json.AsObject.ToDictionary(val => val.Key, val => this.ValueType.ConstrainJson(val.Value)));
        }

        protected override ImmutableJson DeepRebase(ImmutableJson json, ImmutableJson oldBase, ImmutableJson newBase)
        {
            var result = new JsonObject();
            var keys = json.AsObject.Keys.Concat(oldBase.AsObject.Keys).Concat(newBase.AsObject.Keys).Distinct().Order();
            foreach (var key in keys)
            {
                json.AsObject.TryGetValue(key, out var jsonValue);
                oldBase.AsObject.TryGetValue(key, out var oldBaseValue);
                newBase.AsObject.TryGetValue(key, out var newBaseValue);
                if (jsonValue == null)
                {
                    if (oldBaseValue == null)
                        result[key] = newBaseValue!;
                }
                else if (oldBaseValue == null)
                {
                    result[key] = jsonValue;
                }
                else if (newBaseValue == null)
                {
                    if (!ImmutableJson.Equals(jsonValue, oldBaseValue))
                        result[key] = jsonValue;
                }
                else
                {
                    result[key] = ValueTypePerKey(key).ConstrainedRebase(jsonValue, oldBaseValue, newBaseValue);
                }
            }
            return result;
        }
    }

    public class RecordSchemaType : SchemaType
    {
        public SchemaRecord Record { get; }
        public bool Compact { get; }

        public IEnumerable<SchemaField> AllFields
        {
            get { return Record.AllFields.Where(p => !p.IsTag); }
        }

        public RecordSchemaType(SchemaRecord record, bool optional = false, bool compact = false, string? help = null)
            : base(optional, help)
        {
            this.Record = record;
            this.Compact = compact;
        }

        public override SchemaTypeKind Kind => SchemaTypeKind.Record;

        public override SchemaType? GetByPath(JsonPath path)
        {
            if (path.Head == null)
                return this;
            return Record.GetByPath(path);
        }

        public override ImmutableJson? TranslateShortcut(IShortcut shortcut)
        {
            foreach (var field in Record.Fields)
            {
                if (field.Type is KeySchemaType)
                {
                    var json = field.Type.TranslateShortcut(shortcut);
                    if (json != null)
                        return new JsonObject { [field.Name] = json };
                }
            }
            return base.TranslateShortcut(shortcut);
        }

        public override bool Equals(SchemaType? other)
        {
            if (other is RecordSchemaType that)
                return this.Kind == that.Kind && this.Optional == that.Optional && this.Help == that.Help && this.Record == that.Record;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), Record);
        }

        public override string ToString() => Record.Name;

        protected override ImmutableJson ConstrainJsonNonOptional(ImmutableJson? json)
        {
            var result = new JsonObject();
            foreach (var field in AllFields)
            {
                var value = SafeJson.GetField(json, field.Name);
                result[field.Name] = field.Type.ConstrainJson(value);
            }
            return result;
        }

        protected override ImmutableJson DeepRebase(ImmutableJson json, ImmutableJson oldBase, ImmutableJson newBase)
        {
            if (!json.IsObject)
                return newBase;
            var result = new JsonObject();
            foreach (var field in AllFields)
            {
                var jsonField = json[field.Name];
                var oldBaseField = oldBase[field.Name];
                var newBaseField = newBase[field.Name];
                var resultField = field.Type.ConstrainedRebase(jsonField, oldBaseField, newBaseField);
                if (!resultField.IsNull)
                    result[field.Name] = resultField;
            }
            return result;
        }
    }

    public class VariantSchemaType : SchemaType
    {
        public SchemaVariant Variant { get; }

        public VariantSchemaType(SchemaVariant variant, bool optional = false, string? help = null)
            : base(optional, help)
        {
            this.Variant = variant;
        }

        public override SchemaTypeKind Kind => SchemaTypeKind.Variant;

        public override SchemaType? GetByPath(JsonPath path)
        {
            if (path.Head == null)
                return this;
            var node = path.Head as JsonObjectPathNode;
            if (node == null)
                return null;
            var field = Variant.AllChildrenFields.FirstOrDefault(f => f.Name == node.Key);
            return field?.Type.GetByPath(path.Tail);
        }

        public override ImmutableJson? TranslateShortcut(IShortcut shortcut)
        {
            foreach (var field in Variant.Fields)
            {
                if (field.Type is KeySchemaType)
                {
                    var json = field.Type.TranslateShortcut(shortcut);
                    if (json != null)
                        return new JsonObject { [field.Name] = json };
                }
            }
            return base.TranslateShortcut(shortcut);
        }

        public override bool Equals(SchemaType? other)
        {
            if (other is VariantSchemaType that)
                return this.Kind == that.Kind && this.Optional == that.Optional && this.Help == that.Help && this.Variant == that.Variant;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), Variant);
        }

        public override string ToString() => Variant.Name;

        protected override ImmutableJson ConstrainJsonNonOptional(ImmutableJson? json)
        {
            var result = new JsonObject();
            var tagValue = Variant.TagField.Type.ConstrainJson(SafeJson.GetField(json, Variant.Tag));
            result[Variant.Tag] = tagValue;
            var record = Variant.GetChild(tagValue.AsString);
            if (record != null)
            {
                foreach (var field in record.AllFields)
                {
                    var value = SafeJson.GetField(json, field.Name);
                    result[field.Name] = field.Type.ConstrainJson(value);
                }
            }
            return result;
        }

        protected override ImmutableJson DeepRebase(ImmutableJson json, ImmutableJson oldBase, ImmutableJson newBase)
        {
            var jsonTag = Variant.TagField.Type.ConstrainJson(json[Variant.Tag]);
            var oldBaseTag = Variant.TagField.Type.ConstrainJson(oldBase[Variant.Tag]);
            if (!ImmutableJson.Equals(jsonTag, oldBaseTag))
                return newBase; // TODO: is that correct?
            var newBaseTag = Variant.TagField.Type.ConstrainJson(newBase[Variant.Tag]);
            var record = Variant.GetChild(jsonTag.AsString);
            if (record == null)
                return newBase;
            if (jsonTag != newBaseTag)
                return json;
            var result = new JsonObject();
            foreach (var field in record.AllFields)
            {
                var jsonField = json[field.Name];
                var oldBaseField = oldBase[field.Name];
                var newBaseField = newBase[field.Name];
                var resultField = field.Type.ConstrainedRebase(jsonField, oldBaseField, newBaseField);
                if (!resultField.IsNull)
                    result[field.Name] = resultField;
            }
            return result;
        }
    }

    public class LocalizedSchemaType : SchemaType
    {
        public RecordSchemaType RecordType { get; }

        public override bool IsAtomic => base.IsAtomic || RecordType.IsAtomic;

        public LocalizedSchemaType(RecordSchemaType recordType, bool optional = false, string? help = null)
            : base(optional, help)
        {
            this.RecordType = recordType;
        }

        public override SchemaTypeKind Kind => SchemaTypeKind.Localized;

        public override SchemaType? GetByPath(JsonPath path)
        {
            return RecordType.GetByPath(path);
        }

        public override bool Equals(SchemaType? other)
        {
            if (other is LocalizedSchemaType that)
                return this.Kind == that.Kind && this.Optional == that.Optional && this.Help == that.Help && this.RecordType.Equals(that.RecordType);
            else
                return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), RecordType);
        }

        public override string ToString() => RecordType.Name;

        protected override ImmutableJson ConstrainJsonNonOptional(ImmutableJson? json)
        {
            return RecordType.ConstrainJson(json);
        }

        protected override ImmutableJson DeepRebase(ImmutableJson json, ImmutableJson oldBase, ImmutableJson newBase)
        {
            return RecordType.ConstrainedRebase(json, oldBase, newBase);
        }
    }

    public abstract class SimpleSchemaType<T> : SchemaType
    {
        protected SimpleSchemaType(bool optional, string? help, Optional<T> schemaDefault)
            : base(optional, help)
        {
            SchemaDefault = schemaDefault;
        }

        public override bool IsAtomic => true;

        public override Type Type => typeof(T);

        public Optional<T> SchemaDefault { get; }

        public virtual T Default => SchemaDefault.GetValueOrDefault();

        public virtual bool ValueEquals(T value1, T value2)
        {
            return Equals(value1, value2);
        }

        public virtual bool IsValid(T value) => true;

        public bool IsOriginalValue(T value, ImmutableJson? originalJson, bool isJsonKey)
        {
            if (originalJson == null)
            {
                return SchemaDefault.HasValue && ValueEquals(value, SchemaDefault.Value);
            }
            else if (isJsonKey)
            {
                if (originalJson.IsString)
                    return ConvertFromJsonKey(originalJson.AsString, out var originalValue) && ValueEquals(value, originalValue);
                else
                    return false;
            }
            else
            {
                return ConvertFromJson(originalJson, out var originalValue) && ValueEquals(value, originalValue);
            }
        }

        public abstract bool ConvertFromJson(ImmutableJson? json, [MaybeNullWhen(false)] out T value);

        public abstract ImmutableJson ConvertToJson(T value);

        public virtual bool ConvertFromJsonKey(string str, out T value) => throw new NotSupportedException();

        public virtual string ConvertToJsonKey(T value) => throw new NotSupportedException();

        // Used for clipboard paste and csv import
        public abstract bool ConvertFromString(string str, [MaybeNullWhen(false)] out T value);

        // Used for clipboard copy and csv export
        public abstract string ConvertToString(T value);

        // Used for pasting
        public virtual T TranslateValue(T value) => value;

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), SchemaDefault);
        }

        protected override ImmutableJson ConstrainJsonNonOptional(ImmutableJson? json)
        {
            if (ConvertFromJson(json, out var value))
                return ConvertToJson(value);
            else
                return ConvertToJson(Default);
        }

        public virtual IComparer<T> Comparer => Comparer<T>.Default;
    }

    public class IntSchemaType : SimpleSchemaType<int>
    {
        public IntSchemaType(bool optional = false, string? help = null, int? @default = null, int? minValue = null, int? maxValue = null)
            : base(optional, help, @default.ToOptional())
        {
            this.MinValue = minValue ?? int.MinValue;
            this.MaxValue = maxValue ?? int.MaxValue;
            this.IsSlider = minValue.HasValue && maxValue.HasValue;
        }

        public bool IsSlider { get; }

        public override SchemaTypeKind Kind => SchemaTypeKind.Int;

        public NumberFormatInfo NumberFormat => CultureInfo.InvariantCulture.NumberFormat;

        public override bool ConvertFromJson(ImmutableJson? json, out int value)
        {
            if (json != null && json.IsInt)
            {
                value = json.AsInt;
                return true;
            }
            value = Default;
            return false;
        }

        public override ImmutableJson ConvertToJson(int value)
        {
            return value;
        }

        public override bool ConvertFromJsonKey(string str, out int value)
        {
            if (int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                return true;
            value = Default;
            return false;
        }

        public override string ConvertToJsonKey(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public override bool ConvertFromString(string str, out int value)
        {
            return int.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out value) || int.TryParse(str, NumberStyles.Any, CultureInfo.CurrentCulture, out value);
        }

        public override string ConvertToString(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public override bool IsValid(int value) => IsInMinMaxInterval(value);

        public override int Default => SchemaDefault.GetValueOrDefault(IsInMinMaxInterval(0) ? 0 : MinValue);

        public int MinValue { get; }
        public int MaxValue { get; }
        public int Step { get; set; } = 1;
        public string? StringFormat { get; set; }

        private bool IsInMinMaxInterval(int value) => (value >= MinValue) && (value <= MaxValue);

        public override bool Equals(SchemaType? other)
        {
            if (other is IntSchemaType that)
                return this.Kind == that.Kind && this.Optional == that.Optional && this.Help == that.Help && SchemaDefault.Equals(that.SchemaDefault);
            else
                return false;
        }

        public override bool IsComparable => true;
    }

    public class FloatSchemaType : SimpleSchemaType<double>
    {
        public FloatSchemaType(bool optional = false, string? help = null, double? @default = null, double? minValue = null, double? maxValue = null)
            : base(optional, help, @default.ToOptional())
        {
            this.MinValue = minValue ?? double.MinValue;
            this.MaxValue = maxValue ?? double.MaxValue;
            this.IsSlider = minValue.HasValue && maxValue.HasValue;
        }

        public bool IsSlider { get; }
        public Optional<double> Step { get; set; }
        public bool SliderTicks { get; set; }
        public string? StringFormat { get; set; }

        public NumberFormatInfo NumberFormat => CultureInfo.InvariantCulture.NumberFormat;

        public override SchemaTypeKind Kind => SchemaTypeKind.Float;

        public override bool ConvertFromJson(ImmutableJson? json, out double value)
        {
            if (json != null && json.IsNumber)
            {
                value = json.AsNumber;
                return true;
            }
            value = Default;
            return false;
        }

        public override ImmutableJson ConvertToJson(double value)
        {
            return value;
        }

        public override bool ConvertFromJsonKey(string str, out double value)
        {
            if (double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return true;
            value = Default;
            return false;
        }

        public override string ConvertToJsonKey(double value)
        {
            return ConvertToString(value);
        }

        public override bool ConvertFromString(string str, out double value)
        {
            return double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out value) || double.TryParse(str, NumberStyles.Any, CultureInfo.CurrentCulture, out value);
        }

        public override string ConvertToString(double value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public override bool IsValid(double value) => IsInMinMaxInterval(value);
        public override double Default => SchemaDefault.GetValueOrDefault(IsInMinMaxInterval(0) ? 0.0 : MinValue);

        public double MinValue { get; }

        public double MaxValue { get; }

        private bool IsInMinMaxInterval(double value) => (value >= MinValue) && (value <= MaxValue);

        public override bool ValueEquals(double val1, double val2)
        {
            return Numbers.Compare(val1, val2);
        }

        public override bool Equals(SchemaType? other)
        {
            if (other is FloatSchemaType that)
                return this.Kind == that.Kind && this.Optional == that.Optional && this.Help == that.Help && this.SchemaDefault.Equals(that.SchemaDefault);
            else
                return false;
        }

        public override bool IsComparable => true;
    }

    public class BoolSchemaType : SimpleSchemaType<bool>
    {
        public BoolSchemaType(bool optional = false, string? help = null, bool? @default = null)
            : base(optional, help, @default.ToOptional())
        {
        }

        public override SchemaTypeKind Kind => SchemaTypeKind.Bool;

        public override bool ConvertFromJson(ImmutableJson? json, out bool value)
        {
            if (json != null && json.IsBool)
            {
                value = json.AsBool;
                return true;
            }
            value = Default;
            return false;
        }

        public override ImmutableJson ConvertToJson(bool value)
        {
            return value;
        }

        public override bool ConvertFromJsonKey(string str, out bool value)
        {
            if (str == "true")
            {
                value = true;
                return true;
            }
            else if (str == "false")
            {
                value = false;
                return true;
            }
            else
            {
                value = Default;
                return false;
            }
        }

        public override string ConvertToJsonKey(bool value)
        {
            return value ? "true" : "false";
        }

        public override bool ConvertFromString(string str, out bool value)
        {
            if (string.Compare(str, bool.TrueString, StringComparison.OrdinalIgnoreCase) == 0 || str == "1")
            {
                value = true;
                return true;
            }
            else if (string.Compare(str, bool.FalseString, StringComparison.OrdinalIgnoreCase) == 0 || str == "0")
            {
                value = false;
                return true;
            }
            else
            {
                value = Default;
                return false;
            }
        }

        public override string ConvertToString(bool value)
        {
            return value ? "true" : "false";
        }

        public override bool Equals(SchemaType? other)
        {
            if (other is BoolSchemaType that)
                return this.Kind == that.Kind && this.Optional == that.Optional && this.Help == that.Help && this.SchemaDefault.Equals(that.SchemaDefault);
            else
                return false;
        }
    }

    public class StringSchemaType : SimpleSchemaType<string?>
    {
        public enum StringSpecies
        {
            Simple,
            RefAsset,
            RefLink,
            Slot,
        }

        public bool NotEmpty { get; }

        public bool UnrealClassPath { get; set; }
        public bool UnrealAssetPath { get; set; }

        public StringSchemaType(bool optional = false, string? help = null, string? @default = null, bool notEmpty = false)
            : base(optional, help, @default.ToOptional())
        {
            this.NotEmpty = notEmpty;
        }

        public StringSpecies Species { get; set; }

        public string? ReferenceSourceId { get; set; }
        public bool UniqueReference { get; set; }
        public string? ReferenceTargetId { get; set; }
        public bool RequireValidReference { get; set; }

        public override bool IsComparable => true;

        public override SchemaTypeKind Kind => SchemaTypeKind.String;

        public override bool IsValid(string? value)
        {
            return NotEmpty ? !string.IsNullOrEmpty(value) : value != null;
        }

        public override bool ConvertFromJson(ImmutableJson? json, out string? value)
        {
            if (json != null && json.IsString)
            {
                value = json.AsString.Replace("\n", "\\n", StringComparison.Ordinal);
                return true;
            }
            else
            {
                value = Default;
                return false;
            }
        }

        public override ImmutableJson ConvertToJson(string? value)
        {
            return value?.Replace("\\n", "\n", StringComparison.Ordinal) ?? ImmutableJson.Null;
        }

        public override bool ConvertFromJsonKey(string str, [MaybeNullWhen(false)] out string? value)
        {
            value = str ?? Default;
            return str != null;
        }

        public override string ConvertToJsonKey(string? value) => value ?? string.Empty;

        public override bool ConvertFromString(string str, [MaybeNullWhen(false)] out string value)
        {
            value = str ?? Default;
            return str != null;
        }

        public override string ConvertToString(string? value)
        {
            return value ?? string.Empty;
        }

        public override string Default => SchemaDefault.GetValueOrDefault(string.Empty);

        public override bool Equals(SchemaType? other)
        {
            if (other is StringSchemaType that)
                return this.Kind == that.Kind && this.Optional == that.Optional && this.Help == that.Help && this.SchemaDefault.Equals(that.SchemaDefault) && this.NotEmpty == that.NotEmpty;
            else
                return false;
        }

        public override string TranslateValue(string? value)
        {
            if (UnrealClassPath && value != null && value.StartsWith("Blueprint'", StringComparison.OrdinalIgnoreCase) && value.EndsWith("'", StringComparison.Ordinal))
                return value[10..^1] + "_C";
            else
                return base.TranslateValue(value);
        }
    }

    public class PathSchemaType : StringSchemaType
    {
        public string? Root { get; }
        public string? DefaultPath { get; }
        public string? Extension { get; }
        public bool IncludeExtension { get; }
        public bool Preview { get; init; }
        public double? PreviewWidth { get; init; }
        public double? PreviewHeight { get; init; }

        public ProjectSettings? ProjectSettings { get; }
        public PathSchemaType(Igor.Schema.PathOptions pathOptions, ProjectSettings? projectSettings, bool optional = false, string? help = null, string? @default = null, bool notEmpty = false)
            : base(optional, help, @default, notEmpty)
        {
            ProjectSettings = projectSettings;
            Root = pathOptions.Root;
            DefaultPath = pathOptions.DefaultPath;
            Extension = pathOptions.Extension?.TrimStart('.');
            IncludeExtension = pathOptions.IncludeExtension ?? true;
        }

        public override SchemaTypeKind Kind => SchemaTypeKind.Path;

        public override bool Equals(SchemaType? other)
        {
            if (other is PathSchemaType that)
                return this.Kind == that.Kind && this.Optional == that.Optional && this.Help == that.Help && this.Default == that.Default;
            else
                return false;
        }

        public string? GetRelativeFileName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            var filename = value;
            if (UnrealClassPath || UnrealAssetPath)
                filename = value.TryReplacePrefix("/Game", "Content", StringComparison.OrdinalIgnoreCase);

            if (UnrealClassPath || UnrealAssetPath)
            {
                filename = System.IO.Path.ChangeExtension(filename, ".uasset");
            }
            else if (!IncludeExtension && string.IsNullOrEmpty(System.IO.Path.GetExtension(filename)) && !string.IsNullOrEmpty(Extension))
            {
                filename = $"{filename}.{Extension}";
            }

            return System.IO.Path.Join(Root, filename);
        }
    }

    public class SelectStringSchemaType : StringSchemaType, IMultiSelectItemSchemaType
    {
        public SelectStringSchemaType(IObservableDocument sourceDocument, JsonPath sourcePath, bool optional = false, string? help = null, string? @default = null)
            : base(optional, help, @default)
        {
            this.sourceDocument = sourceDocument;
            SourcePath = sourcePath;
            UpdateItems(sourceDocument.Value);
            documentSubscription = sourceDocument.Subscribe(UpdateItems);
        }

        private readonly IDisposable documentSubscription;
        private readonly IObservableDocument sourceDocument;

        public override SchemaTypeKind Kind => SchemaTypeKind.SelectString;

        public JsonPath SourcePath { get; }

        public ObservableCollection<string> Items { get; } = new ObservableCollection<string>();

        public IReadOnlyList<string> MultiSelectItems => Items;

        private void UpdateItems(IDocument? document)
        {
            if (document != null)
            {
                try
                {
                    Items.SynchronizeOrdered(document.Json.Fetch(SourcePath).AsArray.Select(val => val.AsString));
                }
                catch
                {
                    Items.Clear();
                }
            }
        }

        public override bool IsValid(string? value)
        {
            return value != null && Items.Contains(value);
        }

        public override bool Equals(SchemaType? other)
        {
            if (other is SelectStringSchemaType that)
                return this.Kind == that.Kind && this.Optional == that.Optional && this.Help == that.Help && this.Default == that.Default && this.SourcePath.Equals(that.SourcePath) && this.sourceDocument == that.sourceDocument;
            else
                return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = base.GetHashCode();
                hash = (hash * 16777619) ^ SourcePath.GetHashCode();
                if (sourceDocument != null)
                    hash = (hash * 16777619) ^ sourceDocument.GetHashCode();
                return hash;
            }
        }
    }

    public class TextSchemaType : StringSchemaType
    {
        public string? Syntax { get; }
        public string? SyntaxFile => Syntax == null ? null : $"SyntaxHighlight\\{Syntax}.xshd";

        public TextSchemaType(bool optional = false, string? help = null, string? @default = null, bool notEmpty = false, string? syntax = null)
            : base(optional, help, @default, notEmpty)
        {
            Syntax = syntax;
        }

        public override SchemaTypeKind Kind => SchemaTypeKind.Text;

        public override bool ConvertFromJson(ImmutableJson? json, out string? value)
        {
            if (json != null && json.IsString)
            {
                value = string.Join(Environment.NewLine, json.AsString.Split('\n').Select(s => s.Trim('\r')));
                return true;
            }
            else
            {
                value = Default;
                return false;
            }
        }

        public override ImmutableJson ConvertToJson(string? value)
        {
            return value?.Replace(Environment.NewLine, "\n", StringComparison.Ordinal) ?? ImmutableJson.Null;
        }

        public override bool Equals(SchemaType? other)
        {
            if (other is TextSchemaType that)
                return this.Kind == that.Kind && this.Optional == that.Optional && this.Help == that.Help && this.Default == that.Default;
            else
                return false;
        }
    }

    public class BinarySchemaType : SimpleSchemaType<byte[]?>
    {
        public override SchemaTypeKind Kind => SchemaTypeKind.Binary;

        public BinarySchemaType(bool optional = false, string? help = null)
            : base(optional, help, Hercules.Optional.None<byte[]?>())
        {
        }

        public override bool IsValid(byte[]? value) => value != null;

        public override bool ValueEquals(byte[]? value1, byte[]? value2)
        {
            if (value1 == null)
                return value2 == null;
            if (value2 == null)
                return false;
            return value1.SequenceEqual(value2);
        }

        public override bool ConvertFromJson(ImmutableJson? json, [MaybeNullWhen(false)] out byte[]? value)
        {
            if (json != null && json.IsString)
            {
                value = Json.Serialization.JsonSerializer.Binary.Deserialize(json);
                return true;
            }
            else
            {
                value = Default;
                return false;
            }
        }

        public override ImmutableJson ConvertToJson(byte[]? value)
        {
            return value != null ? Json.Serialization.JsonSerializer.Binary.Serialize(value) : ImmutableJson.Null;
        }

        public override bool ConvertFromString(string str, out byte[] value)
        {
            throw new InvalidOperationException();
        }

        public override string ConvertToString(byte[]? value)
        {
            throw new InvalidOperationException();
        }

        public override bool Equals(SchemaType? other)
        {
            if (other is BinarySchemaType that)
                return this.Kind == that.Kind && this.Optional == that.Optional && this.Help == that.Help;
            else
                return false;
        }
    }

    public class DateTimeSchemaType : SimpleSchemaType<DateTime>
    {
        public static readonly CultureInfo Culture = new("en-US")
        {
            DateTimeFormat =
            {
                ShortDatePattern = "yyyy-MM-dd",
                ShortTimePattern = "HH:mm",
                LongDatePattern = "yyyy-MM-dd",
                LongTimePattern = "HH:mm:ss",
                FullDateTimePattern = "yyyy-MM-dd HH:mm:ss"
            }
        };

        public IReadOnlyObservableValue<TimeZoneInfo> TimeZone { get; }

        public DateTimeSchemaType(IReadOnlyObservableValue<TimeZoneInfo> timeZone, bool optional = false, string? help = null)
            : base(optional, help, Hercules.Optional.None<DateTime>())
        {
            TimeZone = timeZone;
        }

        public override SchemaTypeKind Kind => SchemaTypeKind.DateTime;

        public override bool ConvertFromJson(ImmutableJson? json, out DateTime value)
        {
            if (json != null && json.IsString && DateTime.TryParse(json.AsString, out value))
            {
                value = DateTime.SpecifyKind(value, DateTimeKind.Utc);
                return true;
            }
            value = Default;
            return false;
        }

        public override ImmutableJson ConvertToJson(DateTime value)
        {
            return value.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        }

        public override bool ConvertFromString(string str, out DateTime value)
        {
            return ConvertFromString(str, TimeZone.Value, out value);
        }

        public static bool ConvertFromString(string str, TimeZoneInfo timeZone, out DateTime value)
        {
            if (DateTime.TryParse(str, CultureInfo.InstalledUICulture, DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out value)
                || DateTime.TryParse(str, CultureInfo.CurrentUICulture, DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out value)
                || DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out value)
                || DateTime.TryParseExact(str, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out value)
                || DateTime.TryParseExact(str, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out value))
            {
                value = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), timeZone);
                return true;
            }
            return false;
        }

        public override string ConvertToString(DateTime value)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(value, TimeZone.Value).ToString(Culture);
        }

        public override DateTime Default => DateTime.UtcNow;

        public override bool Equals(SchemaType? other)
        {
            if (other is DateTimeSchemaType that)
                return this.Kind == that.Kind && this.Optional == that.Optional && this.Help == that.Help;
            else
                return false;
        }

        public override bool ValueEquals(DateTime value1, DateTime value2)
        {
            return Math.Abs((value1 - value2).TotalSeconds) < 1;
        }

        public override string ToString() => "DateTime";
    }

    public class KeySchemaType : SimpleSchemaType<string?>
    {
        public SchemafulDatabase SchemafulDatabase { get; }
        readonly string? category;
        readonly string? @interface;

        public KeySchemaType(SchemafulDatabase schemafulDatabase, string? category = null, string? @interface = null, bool optional = false, string? help = null)
            : base(optional, help, Hercules.Optional.None<string?>())
        {
            this.SchemafulDatabase = schemafulDatabase;
            this.category = category;
            this.@interface = @interface;
        }

        public override SchemaTypeKind Kind => SchemaTypeKind.Key;

        public IEnumerable<IDocument> Items
        {
            get
            {
                if (category != null)
                {
                    var cat = SchemafulDatabase.GetCategory(category);
                    return cat?.Documents ?? SchemafulDatabase.SchemafulDocuments;
                }
                else if (@interface != null)
                    return SchemafulDatabase.GetDocumentsByInterface(@interface);
                else
                    return SchemafulDatabase.SchemafulDocuments;
            }
        }

        public override bool IsValid(string? value)
        {
            if (value == null)
                return false;
            var items = Items;
            if (items is CompositeCollection)
                items = items.Cast<CollectionContainer>().SelectMany(c => c.Collection.OfType<IDocument>());
            var document = items.Cast<IDocument>().FirstOrDefault(doc => doc.DocumentId == value);
            return document != null;
        }

        public override bool ConvertFromJson(ImmutableJson? json, [MaybeNullWhen(false)] out string? value)
        {
            if (json != null && json.IsString)
            {
                value = json.AsString;
                return true;
            }
            else
            {
                value = Default;
                return false;
            }
        }

        public override ImmutableJson ConvertToJson(string? value)
        {
            return value ?? ImmutableJson.Null;
        }

        public override bool ConvertFromJsonKey(string str, [MaybeNullWhen(false)] out string? value)
        {
            value = str ?? Default;
            return str != null;
        }

        public override string ConvertToJsonKey(string? value) => value ?? string.Empty;

        public override bool ConvertFromString(string str, [MaybeNullWhen(false)] out string? value)
        {
            value = str ?? Default;
            return str != null;
        }

        public override string ConvertToString(string? value) => value ?? string.Empty;

        public override string? Default => null;

        public override ImmutableJson? TranslateShortcut(IShortcut shortcut)
        {
            if (shortcut is DocumentShortcut documentShortcut && IsValid(documentShortcut.DocumentId))
                return documentShortcut.DocumentId;
            else
                return null;
        }

        public override bool Equals(SchemaType? other)
        {
            if (other is KeySchemaType that)
                return this.Kind == that.Kind && this.Optional == that.Optional && this.Help == that.Help && this.category == that.category && this.@interface == that.@interface;
            else
                return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = base.GetHashCode();
                if (category != null)
                    hash = (hash * 16777619) ^ category.GetHashCode(StringComparison.Ordinal);
                if (@interface != null)
                    hash = (hash * 16777619) ^ @interface.GetHashCode(StringComparison.Ordinal);
                return hash;
            }
        }

        public override bool IsComparable => true;
    }

    public class EnumSchemaType : SimpleSchemaType<string?>, IMultiSelectItemSchemaType
    {
        public SchemaEnum Enum { get; }

        public EnumSchemaType(SchemaEnum @enum, bool optional = false, string? help = null, string? @default = null)
            : base(optional, help, @default.ToOptional())
        {
            this.Enum = @enum;
        }

        public override SchemaTypeKind Kind => SchemaTypeKind.Enum;
        public override bool IsComparable => true;
        public override IComparer<string?> Comparer => Enum;

        public override bool IsValid(string? value) => Enum.Values.Contains(value);

        public override bool ConvertFromJson(ImmutableJson? json, out string? value)
        {
            if (json != null && json.IsString)
            {
                value = json.AsString;
                return true;
            }
            else
            {
                value = Default;
                return false;
            }
        }

        public override ImmutableJson ConvertToJson(string? value)
        {
            return string.IsNullOrEmpty(value) ? ImmutableJson.Null : value;
        }

        public override bool ConvertFromJsonKey(string str, out string? value)
        {
            value = str ?? Default;
            return str != null;
        }

        public override string ConvertToJsonKey(string? value) => value ?? string.Empty;

        public override bool ConvertFromString(string str, out string value)
        {
            value = str ?? Default;
            return str != null;
        }

        public override string ConvertToString(string? value) => value ?? string.Empty;

        public override bool Equals(SchemaType? other)
        {
            if (other is EnumSchemaType that)
                return this.Kind == that.Kind && this.Optional == that.Optional && this.Help == that.Help && this.SchemaDefault.Equals(that.SchemaDefault) && this.Enum == that.Enum;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), Enum);
        }

        public override string ToString() => Enum.Name;
        public IReadOnlyList<string> MultiSelectItems => Enum.Values;
    }

    public class MultiSelectSchemaType : SimpleSchemaType<IReadOnlyList<string>?>
    {
        public MultiSelectSchemaType(IMultiSelectItemSchemaType itemSchemaType, bool optional, string? help, Optional<IReadOnlyList<string>> schemaDefault) : base(optional, help, schemaDefault)
        {
            ItemSchemaType = itemSchemaType;
        }

        public IReadOnlyList<string> Items => ItemSchemaType.MultiSelectItems;

        public override SchemaTypeKind Kind => SchemaTypeKind.MultiSelect;

        public IMultiSelectItemSchemaType ItemSchemaType { get; }

        public override bool ConvertFromJson(ImmutableJson? json, [MaybeNullWhen(false)] out IReadOnlyList<string> value)
        {
            if (json != null && json.IsArray)
            {
                value = json.AsArray.Where(v => v.IsString).Select(v => v.AsString).ToList();
                return true;
            }
            value = null;
            return false;
        }

        public override bool ConvertFromString(string str, [MaybeNullWhen(false)] out IReadOnlyList<string> value)
        {
            value = str.Split(",").Select(v => v.Trim()).ToList();
            return true;
        }

        public override ImmutableJson ConvertToJson(IReadOnlyList<string>? value)
        {
            return value == null ? ImmutableJson.Null : new JsonArray(value.Select(ImmutableJson.Create));
        }

        public override string ConvertToString(IReadOnlyList<string>? value)
        {
            return value == null ? string.Empty : string.Join(",", value);
        }

        public override bool Equals(SchemaType? other)
        {
            return other is MultiSelectSchemaType that && that.ItemSchemaType.Equals(ItemSchemaType);
        }

        public override bool IsValid(IReadOnlyList<string>? value)
        {
            if (value == null)
                return false;
            return value.All(v => Items.Contains(v));
        }

        public override bool ValueEquals(IReadOnlyList<string>? value1, IReadOnlyList<string>? value2)
        {
            if (value1 is null && value2 is null)
                return true;
            if (value1 is not null && value2 is not null)
                return value1.Count == value2.Count && value1.All(v => value2.Contains(v));
            return false;
        }
    }

    public class JsonSchemaType : SchemaType
    {
        public override SchemaTypeKind Kind => SchemaTypeKind.Json;

        public JsonSchemaType(bool optional = false, string? help = null)
            : base(optional, help)
        {
        }

        public override bool Equals(SchemaType? other)
        {
            if (other is JsonSchemaType that)
                return this.Kind == that.Kind && this.Optional == that.Optional && this.Help == that.Help;
            else
                return false;
        }

        protected override ImmutableJson ConstrainJsonNonOptional(ImmutableJson? json)
        {
            return json ?? ImmutableJson.Null;
        }
    }

    public abstract class CustomSchemaType : SchemaType
    {
        public SchemaType ContentType { get; }

        public override bool IsAtomic => base.IsAtomic || ContentType.IsAtomic;

        public abstract string Tag { get; }

        public override SchemaTypeKind Kind => SchemaTypeKind.Custom;

        protected CustomSchemaType(SchemaType contentType, bool optional = false, string? help = null)
            : base(optional, help)
        {
            this.ContentType = contentType;
        }

        public override SchemaType? GetByPath(JsonPath path)
        {
            return ContentType.GetByPath(path);
        }

        public override bool Equals(SchemaType? other)
        {
            if (other is CustomSchemaType that)
                return this.Kind == that.Kind && this.Optional == that.Optional && this.Help == that.Help && this.ContentType.Equals(that.ContentType);
            else
                return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), ContentType);
        }

        protected override ImmutableJson ConstrainJsonNonOptional(ImmutableJson? json)
        {
            return ContentType.ConstrainJson(json);
        }

        protected override ImmutableJson DeepRebase(ImmutableJson json, ImmutableJson oldBase, ImmutableJson newBase)
        {
            return ContentType.ConstrainedRebase(json, oldBase, newBase);
        }

        public override string ToString() => ContentType.Name;
    }
}