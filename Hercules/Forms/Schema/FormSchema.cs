using System;
using System.Collections.Generic;
using System.Linq;

namespace Hercules.Forms.Schema
{
    public sealed class SchemaGroup
    {
        public string Name { get; }
        public IReadOnlyList<SchemaRecord> Records { get; }

        public SchemaGroup(string name, IReadOnlyList<SchemaRecord> records)
        {
            Name = name;
            Records = records;
        }
    }

    public sealed class FormSchema
    {
        public SchemaStruct Root { get; }
        public SchemaType RootType { get; }
        public SchemaVariant? Variant { get; }
        public IReadOnlyDictionary<string, SchemaEnum> Enums { get; }
        public IReadOnlyDictionary<string, SchemaStruct> Structs { get; }
        public IReadOnlyDictionary<string, SchemaGroup> Groups { get; }
        public Version SchemaVersion { get; }

        public RecordSchemaType DocumentRoot(string category)
        {
            if (Variant == null)
                throw new InvalidOperationException("Document root is not a variant");
            var record = Variant.GetChild(category);
            if (record == null)
                throw new ArgumentOutOfRangeException(nameof(category));
            return new RecordSchemaType(record);
        }

        public FormSchema(IReadOnlyDictionary<string, SchemaEnum> enums, IReadOnlyDictionary<string, SchemaStruct> structs, SchemaStruct root, Version schemaVersion)
        {
            Enums = enums;
            Structs = structs;
            Root = root;
            SchemaVersion = schemaVersion;
            Variant = Root as SchemaVariant;

            if (Variant != null)
                RootType = new VariantSchemaType(Variant);
            else
                RootType = new RecordSchemaType((SchemaRecord)Root);

            Groups = Variant == null ? new Dictionary<string, SchemaGroup>() : Variant.Children.Where(c => c.Group != null).GroupBy(c => c.Group!).ToDictionary(g => g.Key, g => new SchemaGroup(g.Key, g.ToList()));
        }
    }
}
