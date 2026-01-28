using Hercules.Documents;
using Hercules.Repository;
using Hercules.Shell;
using Hercules.Shortcuts;
using Igor.Schema;
using Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hercules.Forms.Schema
{
    internal sealed class GenericRecordInstance : IEquatable<GenericRecordInstance>
    {
        public string PrototypeName { get; }
        public List<SchemaType> Arguments { get; }

        public GenericRecordInstance(string prototypeName, List<SchemaType> arguments)
        {
            this.PrototypeName = prototypeName;
            this.Arguments = arguments;
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(PrototypeName);
            foreach (var argument in Arguments)
                hash.Add(argument.GetHashCode());
            return hash.ToHashCode();
        }

        public override bool Equals(object? obj)
        {
            if (obj is GenericRecordInstance that)
                return Equals(that);
            else
                return false;
        }

        public bool Equals(GenericRecordInstance? other)
        {
            if (other is null)
                return false;
            return PrototypeName == other.PrototypeName && Enumerable.SequenceEqual(Arguments, other.Arguments);
        }
    }

    internal sealed class IgorFormSchemaBuilder
    {
        public IgorFormSchemaBuilder(ImmutableJson json, FormSettings formSettings, ProjectSettings? projectSettings, TextSizeService textSizeService, ShortcutService shortcutService, CustomTypeRegistry customTypeRegistry, SchemafulDatabase? schemafulDatabase)
        {
            SchemafulDatabase = schemafulDatabase;
            FormSettings = formSettings;
            this.projectSettings = projectSettings;
            TextSizeService = textSizeService;
            this.customTypeRegistry = customTypeRegistry;
            ShortcutService = shortcutService;
            IgorSchema = SchemaJsonSerializer.Instance.Deserialize(json);
            var version = Version.Parse(IgorSchema.Version);

            InitSchema();

            FormSchema = new FormSchema(enums, records, GetStruct(IgorSchema.DocumentType));
        }

        public FormSchema FormSchema { get; }

        private SchemafulDatabase? SchemafulDatabase { get; }
        private FormSettings FormSettings { get; }
        private readonly ProjectSettings? projectSettings;
        private Igor.Schema.Schema IgorSchema { get; }
        private TextSizeService TextSizeService { get; }
        private readonly CustomTypeRegistry customTypeRegistry;
        private ShortcutService ShortcutService { get; }

        private readonly Dictionary<string, SchemaEnum> enums = new Dictionary<string, SchemaEnum>();
        private readonly Dictionary<string, SchemaStruct> records = new Dictionary<string, SchemaStruct>();
        private readonly Dictionary<string, SchemaRecord> localizedRecords = new Dictionary<string, SchemaRecord>();
        private readonly Dictionary<string, SchemaRecord> multilineLocalizedRecords = new Dictionary<string, SchemaRecord>();
        private readonly Dictionary<GenericRecordInstance, SchemaRecord> genericRecordInstances = new Dictionary<GenericRecordInstance, SchemaRecord>();

        void InitSchema()
        {
            foreach (var pair in IgorSchema.CustomTypes)
            {
                var name = pair.Key;
                if (pair.Value is EnumCustomType customType)
                {
                    List<string> values;
                    if (FormSettings.SortEnumValues.Value)
                        values = new List<string>(customType.Values.OrderBy(a => a));
                    else
                        values = new List<string>(customType.Values);
                    var schemaEnum = new SchemaEnum(name, values);
                    enums.Add(name, schemaEnum);
                }
                else if (pair.Value is RecordCustomType recordCustomType && recordCustomType.GenericArguments != null)
                {
                    // do not create SchemaRecord for generic records
                }
                else
                {
                    GetStruct(name);
                }
            }

            foreach (var variant in records.Values.OfType<SchemaVariant>())
            {
                var customType = IgorSchema.CustomTypes[variant.Name];
                foreach (var pair in ((VariantCustomType)customType).Children)
                {
                    var child = (SchemaRecord)GetStruct(pair.Value);
                    child.TagValue = pair.Key;
                    variant.Children.Add(child);
                }
            }

            var emptyGenericArgsCache = new Dictionary<string, SchemaType>();

            foreach (var record in records.Values)
            {
                var customType = (StructCustomType)IgorSchema.CustomTypes[record.Name];
                foreach (var pair in customType.Fields)
                {
                    var name = pair.Key;
                    var descriptor = pair.Value;
                    var isTag = (record is SchemaVariant) && ((SchemaVariant)record).Tag == name;
                    var caption = pair.Key + ":";
                    var field = new SchemaField(pair.Key, caption, Create(descriptor, emptyGenericArgsCache), TextSizeService.GetWidth(caption), isTag);
                    record.Fields.Add(field);
                    if (descriptor.GetBoolMetadata("record_caption"))
                        record.CaptionField = field;
                    if (descriptor.GetBoolMetadata("record_enabled"))
                        record.EnabledField = field;
                    if (descriptor.GetBoolMetadata("key"))
                        field.IsKey = true;
                }

                if (record is SchemaRecord sr)
                {
                    DetectColorRecord(sr);
                    if (customType.TryGetStringMetadata("ai_hint", out var aiHint))
                        sr.AiHint = aiHint;
                }

                var captionPath = customType.GetStringMetadata("caption_path");
                if (captionPath != null)
                    record.CaptionPath = JsonPath.Parse(captionPath);
                var imagePath = customType.GetStringMetadata("image_path");
                if (imagePath != null)
                    record.ImagePath = JsonPath.Parse(imagePath);
            }

            foreach (var localized in localizedRecords.Values)
            {
                var record = GetStruct(localized.Name);
                foreach (var field in record.Fields)
                {
                    if (field.Name == "text" || field.Name == "approved_text")
                    {
                        var newField = new SchemaField(field.Name, field.Caption, FixMultiline(field.Type, false), field.TextWidth, field.IsTag);
                        localized.Fields.Add(newField);
                    }
                    else
                        localized.Fields.Add(field);
                }
            }

            foreach (var localized in multilineLocalizedRecords.Values)
            {
                var record = GetStruct(localized.Name);
                foreach (var field in record.Fields)
                {
                    if (field.Name == "text" || field.Name == "approved_text")
                    {
                        var newField = new SchemaField(field.Name, field.Caption, FixMultiline(field.Type, true), field.TextWidth, field.IsTag);
                        localized.Fields.Add(newField);
                    }
                    else
                        localized.Fields.Add(field);
                }
            }
        }

        SchemaStruct GetStruct(string name)
        {
            if (!records.TryGetValue(name, out var result))
            {
                var customType = IgorSchema.CustomTypes.GetValueOrDefault(name) as StructCustomType;
                if (customType == null)
                    throw new InvalidOperationException($"Type '{name}' not found");
                SchemaVariant? parent = null;
                if (customType.Parent != null)
                    parent = (SchemaVariant)GetStruct(customType.Parent);
                var interfaces = customType.Interfaces;
                if (customType is RecordCustomType recordCustomType)
                {
                    var record = new SchemaRecord(name, parent, interfaces)
                    {
                        Group = recordCustomType.Group
                    };
                    result = record;
                }
                else
                {
                    var variant = (VariantCustomType)customType;
                    result = new SchemaVariant(name, variant.Tag, parent, interfaces);
                }
                records.Add(name, result);
            }
            return result;
        }

        SchemaRecord GetGenericRecordInstance(string prototypeName, IReadOnlyList<Descriptor> arguments, Dictionary<string, SchemaType> genericArgsCache)
        {
            var prototype = IgorSchema.CustomTypes.GetValueOrDefault(prototypeName) as RecordCustomType;
            if (prototype == null)
                throw new InvalidOperationException("Unknown generic record " + prototypeName);
            if (prototype.GenericArguments == null || prototype.GenericArguments.Count != arguments.Count)
                throw new InvalidOperationException("Invalid generic arguments count for " + prototypeName);
            var resolvedArgs = arguments.Select(arg => Create(arg, genericArgsCache)).ToList();
            var instance = new GenericRecordInstance(prototypeName, resolvedArgs);

            if (!genericRecordInstances.TryGetValue(instance, out var result))
            {
                result = new SchemaRecord(prototypeName, null, prototype.Interfaces);
                genericRecordInstances.Add(instance, result);

                var newCache = new Dictionary<string, SchemaType>(genericArgsCache);
                for (int i = 0; i < arguments.Count; i++)
                    newCache[prototype.GenericArguments[i]] = resolvedArgs[i];

                foreach (var pair in prototype.Fields)
                {
                    var descriptor = pair.Value;
                    var caption = pair.Key + ":";
                    var field = new SchemaField(pair.Key, caption, Create(descriptor, newCache), TextSizeService.GetWidth(caption), false);
                    result.Fields.Add(field);
                }
                DetectColorRecord(result);
            }
            return result;
        }

        SchemaRecord GetLocalizedRecord(string name, bool multiline)
        {
            var dict = multiline ? multilineLocalizedRecords : localizedRecords;
            if (!dict.TryGetValue(name, out var result))
            {
                var record = (SchemaRecord)GetStruct(name);
                result = new SchemaRecord(name, record.Parent, record.Interfaces, record.TagValue);
                dict.Add(name, result);
            }
            return result;
        }

        private static SchemaType FixMultiline(SchemaType type, bool multiline)
        {
            return type switch
            {
                StringSchemaType stringType when multiline => new TextSchemaType(stringType.Optional, stringType.Help, stringType.Default, stringType.NotEmpty),
                TextSchemaType textType when !multiline => new StringSchemaType(textType.Optional, textType.Help, textType.Default, textType.NotEmpty),
                _ => type
            };
        }

        SchemaType Create(Descriptor descriptor, Dictionary<string, SchemaType> genericArgs)
        {
            var contentType = CreateContent(descriptor, genericArgs);

            if (descriptor.EditorKey != null && SchemafulDatabase != null)
            {
                if (SchemafulDatabase.AllDocuments.TryGetValue(descriptor.EditorKey, out var document))
                {
                    try
                    {
                        var tag = CouchUtils.GetCategory(document.Json, "editor_type");
                        var customTypeSupport = tag == null ? null : customTypeRegistry.Get(tag);
                        if (customTypeSupport == null)
                            return contentType;
                        else
                            return customTypeSupport.CreateSchemaType(document, contentType, descriptor.Optional, descriptor.Help);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogException(ex);
                    }
                }
                else
                {
                    Logger.LogWarning($"Editor {descriptor.EditorKey} not found");
                }
            }

            contentType.ForceAtomic = descriptor.GetBoolMetadata("atomic");
            return contentType;
        }

        SchemaType CreateContent(Descriptor descriptor, Dictionary<string, SchemaType> genericArgs)
        {
            switch (descriptor.Kind)
            {
                case DescriptorKind.Binary:
                    return new BinarySchemaType(descriptor.Optional, descriptor.Help);

                case DescriptorKind.Bool:
                    {
                        var d = (BoolDescriptor)descriptor;
                        return new BoolSchemaType(d.Optional, d.Help, d.Default);
                    }
                case DescriptorKind.Datetime:
                    return new DateTimeSchemaType(FormSettings.TimeZone, descriptor.Optional, descriptor.Help);

                case DescriptorKind.Dict:
                    {
                        var dict = (DictDescriptor)descriptor;
                        var @default = dict.Default != null && dict.Default.IsObject ? dict.Default.AsObject : null;
                        var keys = dict.Keys?.ToDictionary(p => p.Key, p => Create(p.Value, genericArgs));
                        return new DictSchemaType(Create(dict.Key, genericArgs), Create(dict.Value, genericArgs), keys, dict.Optional, dict.Help, @default)
                        {
                            Compact = dict.GetBoolMetadata("compact")
                        };
                    }
                case DescriptorKind.Enum:
                    {
                        var d = (EnumDescriptor)descriptor;
                        return new EnumSchemaType(enums[d.Name], d.Optional, d.Help, d.Default);
                    }
                case DescriptorKind.Float:
                    {
                        var d = (FloatDescriptor)descriptor;
                        var result = new FloatSchemaType(d.Optional, d.Help, d.Default, d.Min, d.Max);
                        result.Step = d.GetFloatMetadata("step").ToOptional();
                        result.SliderTicks = d.GetBoolMetadata("slider.ticks", false);
                        result.StringFormat = d.GetStringMetadata("format");
                        return result;
                    }
                case DescriptorKind.Int:
                    {
                        var d = (IntDescriptor)descriptor;
                        var result = new IntSchemaType(d.Optional, d.Help, d.Default, d.Min, d.Max);
                        result.Step = d.GetIntMetadata("step", 1);
                        result.StringFormat = d.GetStringMetadata("format");
                        return result;
                    }
                case DescriptorKind.Key:
                    {
                        var d = (KeyDescriptor)descriptor;
                        return new KeySchemaType(SchemafulDatabase, d.Category, d.Interface, d.Optional, d.Help);
                    }
                case DescriptorKind.List:
                    {
                        var list = (ListDescriptor)descriptor;
                        if (list.GetBoolMetadata("multiselect"))
                        {
                            var @default = (list.Default != null && list.Default.IsArray) ? list.Default.AsArray.Select(v => v.AsString).ToList().ToOptional<IReadOnlyList<string>>() : Optional<IReadOnlyList<string>>.None;
                            return new MultiSelectSchemaType((IMultiSelectItemSchemaType)Create(list.Element, genericArgs), list.Optional, list.Help, @default);
                        }
                        else
                        {
                            var @default = list.Default != null && list.Default.IsArray ? list.Default.AsArray : null;
                            return new ListSchemaType(Create(list.Element, genericArgs), ShortcutService, list.Optional, list.Help, @default);
                        }
                    }
                case DescriptorKind.Record:
                    {
                        var d = (RecordDescriptor)descriptor;
                        var record = GetStruct(d.Name);
                        if (record is SchemaRecord)
                            return new RecordSchemaType((SchemaRecord)record, d.Optional, d.Compact || d.GetBoolMetadata("compact"), d.Help);
                        else
                            return new VariantSchemaType((SchemaVariant)record, d.Optional, d.Help);
                    }
                case DescriptorKind.String:
                    {
                        var d = (StringDescriptor)descriptor;
                        StringSchemaType result;
                        if (d.Source != null)
                        {
                            var path = JsonPath.Parse(d.Source);
                            var documentId = (path.Head as JsonObjectPathNode).Key;
                            result = new SelectStringSchemaType(SchemafulDatabase.Database.ObserveDocument(documentId), path.Tail, d.Optional, d.Help, d.Default);
                        }
                        else if (d.Multiline)
                            result = new TextSchemaType(d.Optional, d.Help, d.Default, d.NotEmpty, d.Syntax);
                        else if (d.Path != null)
                        {
                            result = new PathSchemaType(d.Path, projectSettings, d.Optional, d.Help, d.Default, d.NotEmpty)
                            {
                                Preview = d.GetBoolMetadata("preview"),
                                PreviewWidth = d.GetFloatMetadata("preview.width"),
                                PreviewHeight = d.GetFloatMetadata("preview.height")
                            };
                        }
                        else
                            result = new StringSchemaType(d.Optional, d.Help, d.Default, d.NotEmpty);
                        result.UnrealClassPath = d.GetBoolMetadata("unreal_class_path");
                        result.UnrealAssetPath = d.GetBoolMetadata("unreal_asset_path");
                        var referenceId = d.GetStringMetadata("reference.id");
                        if (referenceId != null)
                        {
                            if (d.GetBoolMetadata("reference.source"))
                            {
                                result.ReferenceSourceId = referenceId;
                                result.UniqueReference = d.GetBoolMetadata("reference.unique");
                            }
                            else
                            {
                                result.ReferenceTargetId = referenceId;
                                result.RequireValidReference = d.GetBoolMetadata("reference.validate", true);
                            }
                        }

                        return result;
                    }
                case DescriptorKind.Localized:
                    {
                        var d = (LocalizedDescriptor)descriptor;
                        var record = GetLocalizedRecord(d.Name, d.Multiline);
                        var recordType = new RecordSchemaType(record, d.Optional, false, d.Help);
                        return new LocalizedSchemaType(recordType, d.Optional, d.Help);
                    }
                case DescriptorKind.Json:
                    {
                        return new JsonSchemaType(descriptor.Optional, descriptor.Help);
                    }
                case DescriptorKind.Custom:
                    {
                        // Obsolete: custom is only used for icons
                        var customDescriptor = (CustomDescriptor)descriptor;
                        return new IntSchemaType(customDescriptor.Optional, customDescriptor.Help);
                    }
                case DescriptorKind.GenericInstance:
                    {
                        var genericDescriptor = (GenericInstanceDescriptor)descriptor;
                        var record = GetGenericRecordInstance(genericDescriptor.Prototype, genericDescriptor.Arguments, genericArgs);
                        return new RecordSchemaType(record, genericDescriptor.Optional, false, genericDescriptor.Help);
                    }
                case DescriptorKind.GenericArgument:
                    {
                        var genericArgDescriptor = (GenericArgumentDescriptor)descriptor;
                        var arg = genericArgs.GetValueOrDefault(genericArgDescriptor.Name);
                        if (arg == null)
                            throw new InvalidOperationException($"Cannot instantiate generic argument {genericArgDescriptor.Name}");
                        return arg;
                    }
                default:
                    throw new InvalidOperationException($"Unknown descriptor kind: {descriptor.Kind}");
            }
        }

        void DetectColorRecord(SchemaRecord record)
        {
            if (record.Fields.Count is not (3 or 4))
                return;

            var red = record.Fields.Find(f => f.Name.Equals("r", StringComparison.OrdinalIgnoreCase) || f.Name.StartsWith("red", StringComparison.OrdinalIgnoreCase));
            if (red == null || red.Type is not (FloatSchemaType or IntSchemaType))
                return;
            var green = record.Fields.Find(f => f.Name.Equals("g", StringComparison.OrdinalIgnoreCase) || f.Name.StartsWith("green", StringComparison.OrdinalIgnoreCase));
            if (green == null || green.Type is not (FloatSchemaType or IntSchemaType))
                return;
            var blue = record.Fields.Find(f => f.Name.Equals("b", StringComparison.OrdinalIgnoreCase) || f.Name.StartsWith("blue", StringComparison.OrdinalIgnoreCase));
            if (blue == null || blue.Type is not (FloatSchemaType or IntSchemaType))
                return;
            SchemaField? alpha = record.Fields.Find(f => f.Name.Equals("a", StringComparison.OrdinalIgnoreCase) || f.Name.StartsWith("alpha", StringComparison.OrdinalIgnoreCase));
            if (alpha != null && alpha.Type is not (FloatSchemaType or IntSchemaType))
                return;

            var colorSchema = new ColorRecordSchema(red, green, blue, alpha);
            record.ColorSchema = colorSchema;
        }
    }
}