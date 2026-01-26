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
    internal struct JsonSchemaObject
    {
        public string? Type;
    }

    internal sealed class JsonFormSchemaBuilder
    {
        public JsonFormSchemaBuilder(ImmutableJson json, FormSettings formSettings, ProjectSettings? projectSettings, IDialogService dialogServise, TextSizeService textSizeService, ShortcutService shortcutService, CustomTypeRegistry customTypeRegistry, SchemafulDatabase? schemafulDatabase)
        {
            SchemafulDatabase = schemafulDatabase;
            this.jsonSchema = json;
            FormSettings = formSettings;
            this.projectSettings = projectSettings;
            DialogService = dialogServise;
            TextSizeService = textSizeService;
            this.customTypeRegistry = customTypeRegistry;
            ShortcutService = shortcutService;

            ParseEnums();
            ParseRecords();

            FormSchema = new FormSchema(enums, records, GetStruct(IgorSchema.DocumentType));
        }

        private void ParseEnums()
        {
            var definitions = jsonSchema["$defs"].AsObject;
            foreach (var definition in definitions)
            {
                if (definition.Value.AsObject.TryGetValue("enum", out var enumValues))
                {
                    var enumType = new SchemaEnum(definition.Key, enumValues.AsArray.Select(v => v.AsString).ToArray());
                    enums.Add(definition.Key, enumType);
                }
            }
        }

        private void ParseRecords()
        {
            Dictionary<string, string> parents = new();
            var definitions = jsonSchema["$defs"].AsObject;
            foreach (var definition in definitions)
            {
                if (definition.Value.AsObject.TryGetValue("oneOf", out var oneOf))
                {
                    foreach (var child in oneOf.AsArray)
                    {
                        if (child.AsObject.TryGetValue("$ref", out var childRef))
                        {
                            var childName = GetNameByRef(childRef.AsString);
                            parents[childName] = definition.Key;
                        }
                    }
                }
            }
            foreach (var definition in definitions)
            {

            }
        }

        private string GetNameByRef(string refName)
        {
            return refName.RemovePrefix("#/$defs/");
        }

        private ImmutableJsonObject GetRef(string @ref)
        {
            var parts = @ref.Split('/');
            ImmutableJson result = ImmutableJson.EmptyObject;
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == "#")
                    result = jsonSchema;
                else
                    result = jsonSchema[parts[i]];
            }
            return result.AsObject;
        }

        private void ParseJsonSchemaObject(ImmutableJsonObject json, ref JsonSchemaObject schema)
        {
            if (json.TryGetValue("$ref", out var refJson) && refJson.IsString)
                ParseJsonSchemaObject(GetRef(refJson.AsString), ref schema);
            if (json.TryGetValue("type", out var typeJson) && typeJson.IsString)
                schema.Type = typeJson.AsString;
        }

        public FormSchema FormSchema { get; }

        private SchemafulDatabase? SchemafulDatabase { get; }
        private FormSettings FormSettings { get; }

        private readonly ImmutableJson jsonSchema;
        private readonly ProjectSettings? projectSettings;
        private Igor.Schema.Schema IgorSchema { get; }
        private TextSizeService TextSizeService { get; }
        private readonly CustomTypeRegistry customTypeRegistry;
        private IDialogService DialogService { get; }
        private ShortcutService ShortcutService { get; }

        private readonly Dictionary<string, SchemaEnum> enums = new Dictionary<string, SchemaEnum>();
        private readonly Dictionary<string, SchemaStruct> records = new Dictionary<string, SchemaStruct>();
        private readonly Dictionary<string, SchemaRecord> localizedRecords = new Dictionary<string, SchemaRecord>();
        private readonly Dictionary<string, SchemaRecord> multilineLocalizedRecords = new Dictionary<string, SchemaRecord>();
        private readonly Dictionary<GenericRecordInstance, SchemaRecord> genericRecordInstances = new Dictionary<GenericRecordInstance, SchemaRecord>();

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