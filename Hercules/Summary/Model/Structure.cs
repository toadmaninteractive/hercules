using Hercules.Forms.Schema;
using Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace Hercules.Summary
{
    public abstract class StructureItem : NotifyPropertyChanged
    {
        protected StructureItem(string name, bool isCategory, bool isFake)
        {
            Name = name;
            IsCategory = isCategory;
            IsFake = isFake;
        }

        public string Name { get; set; }

        public bool IsCategory { get; }

        public bool IsFake { get; }

        public abstract IEnumerable<StructureValue> Collect();

        public abstract void Visit(Func<StructureItem, bool> visitor);

        public abstract StructureItem? GetByPath(JsonPath path);
    }

    public class StructureDummyItem : StructureItem
    {
        public StructureDummyItem()
            : base("Loading...", false, false)
        {
        }

        public override IEnumerable<StructureValue> Collect()
        {
            return Array.Empty<StructureValue>();
        }

        public override void Visit(Func<StructureItem, bool> visitor)
        {
        }

        public override StructureItem? GetByPath(JsonPath path) => null;
    }

    public class StructureCategory : StructureItem
    {
        public StructureCategory(string name, IEnumerable<StructureItem> items, bool isFake = false)
            : base(name, true, isFake)
        {
            Children = new ObservableCollection<StructureItem> { new StructureDummyItem() };
            virtualItems = items;
            IsVirtual = true;
        }

        public bool IsVirtual { get; private set; }

        private readonly IEnumerable<StructureItem> virtualItems;

        public ObservableCollection<StructureItem> Children { get; }

        bool isExpanded = false;

        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                if (value != isExpanded)
                {
                    isExpanded = value;
                    RaisePropertyChanged();
                }

                if (isExpanded && IsVirtual)
                {
                    Children.Clear();
                    Children.AddRange(virtualItems);
                    IsVirtual = false;
                }
            }
        }

        public override IEnumerable<StructureValue> Collect()
        {
            if (!IsVirtual)
                return Children.SelectMany(c => c.Collect());
            else
                return Array.Empty<StructureValue>();
        }

        public override void Visit(Func<StructureItem, bool> visitor)
        {
            if (visitor(this))
            {
                foreach (var child in Children)
                    child.Visit(visitor);
            }
        }

        public override StructureItem? GetByPath(JsonPath path)
        {
            if (path.Head == null)
                return this;

            string? name = path.Head switch
            {
                JsonObjectPathNode objectPathNode => objectPathNode.Key,
                JsonObjectKeyPathNode objectKeyPathNode => objectKeyPathNode.Key,
                JsonArrayPathNode arrayPathNode => arrayPathNode.Index.ToString(CultureInfo.InvariantCulture),
                _ => null,
            };
            IsExpanded = true;
            foreach (var child in Children)
            {
                if (child.IsFake)
                {
                    if (child.IsCategory)
                    {
                        var result = child.GetByPath(path);
                        if (result != null)
                            return result;
                    }
                }
                else if (child.Name == name)
                    return child.GetByPath(path.Tail);
            }
            return null;
        }
    }

    public class StructureValue : StructureItem
    {
        public StructureValue(string name, JsonPath path, SchemaType type)
            : base(name, false, false)
        {
            Path = path;
            Type = type;
        }

        bool isChecked = false;

        public bool IsChecked
        {
            get => isChecked;
            set => SetField(ref isChecked, value);
        }

        public JsonPath Path { get; }

        public SchemaType Type { get; }

        public override IEnumerable<StructureValue> Collect()
        {
            if (isChecked)
                yield return this;
        }

        public override void Visit(Func<StructureItem, bool> visitor)
        {
            visitor(this);
        }

        public override StructureItem GetByPath(JsonPath path) => this;
    }

    public class Structure
    {
        public Structure(FormSchema schema, string category)
        {
            Children = new ObservableCollection<StructureItem>(StructureFactory.CreateFields(JsonPath.Empty, schema.DocumentRoot(category).Record.AllFields.Where(f => !f.IsTag)));
        }

        public ObservableCollection<StructureItem> Children { get; }

        public IEnumerable<StructureValue> Collect()
        {
            return Children.SelectMany(c => c.Collect());
        }

        public IEnumerable<JsonPath> CollectPaths()
        {
            return Collect().Select(val => val.Path);
        }

        public void Visit(Func<StructureItem, bool> visitor)
        {
            foreach (var item in Children)
                item.Visit(visitor);
        }

        public StructureItem? GetByPath(JsonPath path)
        {
            string name = path.Head switch
            {
                JsonObjectPathNode objectPathNode => objectPathNode.Key,
                JsonObjectKeyPathNode objectKeyPathNode => objectKeyPathNode.Key,
                JsonArrayPathNode arrayPathNode => arrayPathNode.Index.ToString(CultureInfo.InvariantCulture),
                _ => string.Empty
            };

            var root = Children.FirstOrDefault(i => i.Name == name);
            return root?.GetByPath(path.Tail);
        }
    }

    public static class StructureFactory
    {
        public static StructureItem? Create(string name, JsonPath path, SchemaType type)
        {
            switch (type)
            {
                case ListSchemaType listSchemaType:
                    {
                        var element = listSchemaType.ItemType;
                        var loader = Enumerable.Range(0, 5).Select(i => Create(i.ToString(CultureInfo.InvariantCulture), path.AppendArray(i), element)).WhereNotNull();
                        return new StructureCategory(name, loader);
                    }
                case DictSchemaType dictSchemaType:
                    {
                        if (dictSchemaType.KeyType.Kind == SchemaTypeKind.Enum)
                        {
                            var loader = ((EnumSchemaType)dictSchemaType.KeyType).Enum.Values.Select(p => Create(p, path.AppendObject(p), dictSchemaType.ValueTypePerKey(p))).WhereNotNull();
                            return new StructureCategory(name, loader);
                        }
                        if (dictSchemaType.KeyType.Kind == SchemaTypeKind.Key)
                        {
                            var keyType = (KeySchemaType)dictSchemaType.KeyType;
                            var loader = keyType.Items.Select(doc => Create(doc.DocumentId, path.AppendObject(doc.DocumentId), dictSchemaType.ValueTypePerKey(doc.DocumentId))).WhereNotNull();
                            return new StructureCategory(name, loader);
                        }
                        if (dictSchemaType.KeyType.Kind == SchemaTypeKind.SelectString)
                        {
                            var loader = ((SelectStringSchemaType)dictSchemaType.KeyType).Items.Select(p => Create(p, path.AppendObject(p), dictSchemaType.ValueTypePerKey(p))).WhereNotNull();
                            return new StructureCategory(name, loader);
                        }
                        return null;
                    }
                case RecordSchemaType recordSchemaType:
                    {
                        return new StructureCategory(name, CreateFields(path, recordSchemaType.AllFields));
                    }
                case VariantSchemaType variantSchemaType:
                    {
                        var fields = CreateFields(path, variantSchemaType.Variant.AllFields);
                        var innerFields = variantSchemaType.Variant.Children.Select(p => new StructureCategory(p.TagValue!, CreateFields(path, p.FieldsUpTo(variantSchemaType.Variant)), isFake: true));
                        return new StructureCategory(name, fields.Concat(innerFields));
                    }
                case LocalizedSchemaType localized:
                    {
                        return Create(name, path, localized.RecordType);
                    }
                case CustomSchemaType custom:
                    {
                        return Create(name, path, custom.ContentType);
                    }
                case BinarySchemaType:
                    return null;

                default:
                    return new StructureValue(name, path, type);
            }
        }

        public static IEnumerable<StructureItem> CreateFields(JsonPath path, IEnumerable<SchemaField> fields)
        {
            return fields.Select(p => Create(p.Name, path.AppendObject(p.Name), p.Type)).WhereNotNull();
        }
    }
}