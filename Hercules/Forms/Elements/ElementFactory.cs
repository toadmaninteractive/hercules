using Hercules.Diagrams;
using Hercules.Documents;
using Hercules.Documents.Editor;
using Hercules.Forms.Schema;
using Json;
using System.Globalization;

namespace Hercules.Forms.Elements
{
    public interface IElementFactory
    {
        Element Create(IContainer parent, SchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction);
        Element CreateKey(IContainer parent, SchemaType type, string jsonKey, string? originalJsonKey, ITransaction transaction);
        Element CreateNotOptional(IContainer parent, SchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction);
        ListItem CreateListItem(ListElement parent, SchemaType itemType, ImmutableJson? json, ImmutableJson? originalJson, int index, int? originalIndex, ITransaction transaction);
    }

    public class ElementFactoryContext
    {
        public DocumentEditorPage? EditorPage { get; }

        public ElementFactoryContext(DocumentEditorPage? editorPage)
        {
            this.EditorPage = editorPage;
        }

        public static ElementFactoryContext Default { get; } = new ElementFactoryContext(null);
    }

    public class ElementFactory : IElementFactory
    {
        private readonly ElementFactoryContext context;
        private readonly CustomTypeRegistry customTypeRegistry;

        public ElementFactory(CustomTypeRegistry customEditorRegistry, ElementFactoryContext context)
        {
            this.customTypeRegistry = customEditorRegistry;
            this.context = context;
        }

        public Element Create(IContainer parent, SchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
        {
            if (type.Optional)
                return new OptionalElement(parent, type, json, originalJson, transaction);
            else
                return CreateNotOptional(parent, type, json, originalJson, transaction);
        }

        public Element CreateKey(IContainer parent, SchemaType type, string jsonKey, string? originalJsonKey, ITransaction transaction)
        {
            var originalJson = originalJsonKey == null ? null : ImmutableJson.Create(originalJsonKey);
            switch (type.Kind)
            {
                case SchemaTypeKind.Bool:
                    return Create(parent, type, jsonKey == "true", originalJson, transaction);

                case SchemaTypeKind.Int:
                    {
                        ImmutableJson? json = null;
                        if (int.TryParse(jsonKey, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                            json = v;
                        return Create(parent, type, json, originalJson, transaction);
                    }
                case SchemaTypeKind.String:
                case SchemaTypeKind.Text:
                case SchemaTypeKind.Path:
                case SchemaTypeKind.SelectString:
                case SchemaTypeKind.Enum:
                case SchemaTypeKind.Key:
                    return Create(parent, type, jsonKey, originalJson, transaction);

                case SchemaTypeKind.Custom:
                    {
                        var customType = (CustomSchemaType)type;
                        var customTypeSupport = customTypeRegistry.Get(customType.Tag);
                        if (customTypeSupport != null)
                            return customTypeSupport.CreateKeyElement(parent, customType, jsonKey, originalJsonKey, transaction, context);
                        else
                            return new InvalidElement(parent, jsonKey, originalJson, transaction);
                    }

                default:
                    return new InvalidElement(parent, jsonKey, originalJson, transaction);
            }
        }

        public Element CreateNotOptional(IContainer parent, SchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
        {
            switch (type.Kind)
            {
                case SchemaTypeKind.Bool:
                    return new BoolElement(parent, (BoolSchemaType)type, json, originalJson, transaction);

                case SchemaTypeKind.Int:
                    return new IntElement(parent, (IntSchemaType)type, json, originalJson, transaction);

                case SchemaTypeKind.Float:
                    return new FloatElement(parent, (FloatSchemaType)type, json, originalJson, transaction);

                case SchemaTypeKind.String:
                    {
                        var stringSchemaType = (StringSchemaType)type;
                        if (stringSchemaType.ReferenceTargetId != null)
                            return new RefElement(parent, stringSchemaType, json, originalJson, transaction);

                        switch (((StringSchemaType)type).Species)
                        {
                            case StringSchemaType.StringSpecies.Slot:
                                return new SlotElement(parent, (StringSchemaType)type, json, originalJson, transaction);
                        }
                        return new StringElement(parent, (StringSchemaType)type, json, originalJson, transaction);
                    }
                case SchemaTypeKind.Path:
                    return new PathElement(parent, (PathSchemaType)type, json, originalJson, transaction);

                case SchemaTypeKind.Text:
                    {
                        var textType = (TextSchemaType)type;
                        if (textType.Syntax == null)
                            return new TextElement(parent, textType, json, originalJson, transaction);
                        else
                            return new AvalonTextElement(parent, textType, json, originalJson, transaction);
                    }

                case SchemaTypeKind.SelectString:
                    return new SelectStringElement(parent, (SelectStringSchemaType)type, json, originalJson, transaction);

                case SchemaTypeKind.MultiSelect:
                    return new MultiSelectElement(parent, (MultiSelectSchemaType)type, json, originalJson, transaction);

                case SchemaTypeKind.Binary:
                    return new BinaryElement(parent, (BinarySchemaType)type, json, originalJson, transaction);

                case SchemaTypeKind.List:
                    return new ListElement(parent, (ListSchemaType)type, json, originalJson, transaction);

                case SchemaTypeKind.Dict:
                    return new DictElement(parent, (DictSchemaType)type, json, originalJson, transaction);

                case SchemaTypeKind.Enum:
                    return new EnumElement(parent, (EnumSchemaType)type, json, originalJson, transaction);

                case SchemaTypeKind.Record:
                    return new RecordElement(parent, (RecordSchemaType)type, json, originalJson, transaction);

                case SchemaTypeKind.Variant:
                    return new VariantElement(parent, (VariantSchemaType)type, json, originalJson, transaction);

                case SchemaTypeKind.Localized:
                    return new LocalizedElement(parent, (LocalizedSchemaType)type, json, originalJson, transaction);

                case SchemaTypeKind.Key:
                    return new KeyElement(parent, (KeySchemaType)type, json, originalJson, transaction);

                case SchemaTypeKind.DateTime:
                    return new DateTimeElement(parent, (DateTimeSchemaType)type, json, originalJson, transaction);

                case SchemaTypeKind.Json:
                    return new JsonElement(parent, (JsonSchemaType)type, json, originalJson, transaction);

                case SchemaTypeKind.Custom:
                    {
                        var customType = (CustomSchemaType)type;
                        var customTypeSupport = customTypeRegistry.Get(customType.Tag);
                        if (customTypeSupport != null)
                            return customTypeSupport.CreateElement(parent, customType, json, originalJson, transaction, context);
                        else
                            return new InvalidElement(parent, json, originalJson, transaction);
                    }
                default:
                    return new InvalidElement(parent, json, originalJson, transaction);
            }
        }

        public ListItem CreateListItem(ListElement parent, SchemaType itemType, ImmutableJson? json, ImmutableJson? originalJson, int index, int? originalIndex, ITransaction transaction)
        {
            if (itemType.IsBlockType())
                return new BlockListItem(parent, itemType, json, originalJson, index, originalIndex, transaction);
            else if (itemType is RecordSchemaType recordSchemaType)
            {
                if (recordSchemaType.Record.IsLink)
                    return new LinkListItem(parent, itemType, json, originalJson, index, originalIndex, transaction);
                if (recordSchemaType.Record.IsReplica)
                    return new ReplicaListItem(parent, itemType, json, originalJson, index, originalIndex, transaction);
            }

            return new ListItem(parent, itemType, json, originalJson, index, originalIndex, transaction);
        }
    }
}