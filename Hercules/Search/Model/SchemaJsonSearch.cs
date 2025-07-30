using Hercules.Forms.Schema;
using Json;

namespace Hercules.Search
{
    public class SchemaJsonSearch
    {
        private readonly ISearchVisitor visitor;

        public SchemaJsonSearch(ISearchVisitor visitor)
        {
            this.visitor = visitor;
        }

        public void Visit(JsonPath path, ImmutableJson data, SchemaType type)
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
                            Visit(path.AppendObjectKey(pair.Key), pair.Key, dictSchemaType.KeyType);
                            Visit(path.AppendObject(pair.Key), pair.Value, dictSchemaType.ValueTypePerKey(pair.Key));
                        }
                    }
                    break;

                case ListSchemaType listSchemaType:
                    if (data.IsArray)
                    {
                        for (int i = 0; i < data.AsArray.Count; i++)
                        {
                            Visit(path.AppendArray(i), data[i], listSchemaType.ItemType);
                        }
                    }
                    break;

                case RecordSchemaType record:
                    if (data.IsObject)
                    {
                        foreach (var field in record.Record.Fields)
                        {
                            if (data.AsObject.TryGetValue(field.Name, out var val) && !val.IsNull)
                            {
                                visitor.VisitPath(path.AppendObjectKey(field.Name), SearchDataType.Field, field.Name);
                                Visit(path.AppendObject(field.Name), val, field.Type);
                            }
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
                                    if (data.AsObject.TryGetValue(field.Name, out var val) && !val.IsNull)
                                    {
                                        visitor.VisitPath(path.AppendObjectKey(field.Name), SearchDataType.Field, field.Name);
                                        Visit(path.AppendObject(field.Name), val, field.Type);
                                    }
                                }
                            }
                        }
                    }
                    break;

                case LocalizedSchemaType localized:
                    Visit(path, data, localized.RecordType);
                    break;

                case StringSchemaType:
                    if (data.IsString)
                        visitor.VisitPath(path, SearchDataType.Text, data.AsString);
                    break;

                case CustomSchemaType custom:
                    Visit(path, data, custom.ContentType);
                    break;

                case KeySchemaType:
                    if (data.IsString)
                        visitor.VisitPath(path, SearchDataType.Key, data.AsString);
                    break;

                case EnumSchemaType:
                    if (data.IsString)
                        visitor.VisitPath(path, SearchDataType.Enum, data.AsString);
                    break;

                case IntSchemaType:
                    if (data.IsInt)
                        visitor.VisitPath(path, SearchDataType.Number, data.ToString());
                    break;

                case FloatSchemaType:
                    if (data.IsNumber)
                        visitor.VisitPath(path, SearchDataType.Number, data.ToString());
                    break;

                case MultiSelectSchemaType multiSelectSchemaType:
                    if (data.IsArray)
                    {
                        for (int i = 0; i < data.AsArray.Count; i++)
                        {
                            Visit(path.AppendArray(i), data[i], (SchemaType)multiSelectSchemaType.ItemSchemaType);
                        }
                    }
                    break;
            }
        }
    }
}