using Hercules.Documents;
using Hercules.Forms.Schema;
using Json;
using System;
using System.Collections.Generic;

namespace Hercules.Localization
{
    public class LocalizationSearch
    {
        private readonly FormSchema schema;
        private readonly Action<IDocument, JsonPath, ImmutableJsonObject> callback;

        public LocalizationSearch(FormSchema schema, Action<IDocument, JsonPath, ImmutableJsonObject> callback)
        {
            this.schema = schema;
            this.callback = callback;
        }

        public void AddDocument(IDocument document)
        {
            Visit(document, JsonPath.Empty, document.Json, schema.RootType);
        }

        public void AddDocuments(IEnumerable<IDocument> documents)
        {
            foreach (var document in documents)
            {
                AddDocument(document);
            }
        }

        private void Visit(IDocument document, JsonPath path, ImmutableJson? data, SchemaType type)
        {
            if (data == null)
                return;

            switch (type)
            {
                case DictSchemaType dictSchemaType:
                    if (data.IsObject)
                    {
                        foreach (var pair in data.AsObject)
                        {
                            Visit(document, path.AppendObjectKey(pair.Key), pair.Key, dictSchemaType.KeyType);
                            Visit(document, path.AppendObject(pair.Key), pair.Value, dictSchemaType.ValueTypePerKey(pair.Key));
                        }
                    }
                    break;

                case ListSchemaType listSchemaType:
                    if (data.IsArray)
                    {
                        for (int i = 0; i < data.AsArray.Count; i++)
                        {
                            Visit(document, path.AppendArray(i), data[i], listSchemaType.ItemType);
                        }
                    }
                    break;

                case RecordSchemaType record:
                    if (data.IsObject)
                    {
                        foreach (var field in record.Record.Fields)
                        {
                            if (data.AsObject.TryGetValue(field.Name, out var val))
                                Visit(document, path.AppendObject(field.Name), val, field.Type);
                        }
                    }
                    break;

                case VariantSchemaType variant:
                    if (data.IsObject)
                    {
                        if (data.AsObject.TryGetValue(variant.Variant.Tag, out var tagJson) && tagJson.IsString)
                        {
                            var tagValue = tagJson.AsString;
                            var child = variant.Variant.GetChild(tagValue);
                            if (child != null)
                            {
                                foreach (var field in child.AllFields)
                                {
                                    if (data.AsObject.TryGetValue(field.Name, out var val))
                                        Visit(document, path.AppendObject(field.Name), val, field.Type);
                                }
                            }
                        }
                    }
                    break;

                case LocalizedSchemaType localized:
                    if (data.IsObject)
                        FoundEntry(document, path, data.AsObject);
                    break;
            }
        }

        private void FoundEntry(IDocument document, JsonPath path, ImmutableJsonObject data)
        {
            var text = data.GetValueSafe("text").AsStringOrNull();
            if (text == null)
                return;
            callback(document, path, data);
        }
    }
}
