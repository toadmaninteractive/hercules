using Hercules.Forms.Schema;
using Json;
using System.Diagnostics.CodeAnalysis;

namespace Hercules.Documents
{
    public interface IMetaSchemaProvider
    {
        bool TryGetSchema(IDocument document, ImmutableJsonObject json, [MaybeNullWhen(false)] out SchemaRecord schema);
    }

    public class MetaSchemaProvider : IMetaSchemaProvider
    {
        public FormSchema FormMetaSchema { get; }
        public FormSchema DiagramMetaSchema { get; }
        public CustomTypeRegistry CustomTypeRegistry { get; }
        public FormSchema ScriptsMetaSchema { get; }

        public MetaSchemaProvider(FormSchema formMetaSchema, FormSchema diagramMetaSchema, CustomTypeRegistry customTypeRegistry, FormSchema scriptsMetaSchema)
        {
            FormMetaSchema = formMetaSchema;
            DiagramMetaSchema = diagramMetaSchema;
            CustomTypeRegistry = customTypeRegistry;
            ScriptsMetaSchema = scriptsMetaSchema;
        }

        public bool TryGetSchema(IDocument document, ImmutableJsonObject json, [MaybeNullWhen(false)] out SchemaRecord schema)
        {
            if (document.DocumentId == CouchUtils.SchemaDocumentId)
            {
                schema = (SchemaRecord)FormMetaSchema.Root;
                return true;
            }
            if (document.DocumentId == CouchUtils.DiagramSchemaDocumentId)
            {
                schema = (SchemaRecord)DiagramMetaSchema.Root;
                return true;
            }

            var scope = CouchUtils.GetScope(json);
            if (scope == "editor")
            {
                var editorTag = CouchUtils.GetCategory(json, "editor_type");
                var customType = editorTag == null ? null : CustomTypeRegistry.Get(editorTag);
                if (customType != null)
                {
                    schema = customType.EditorMetaSchemaRecord;
                    return true;
                }
            }

            if (scope == "script")
            {
                schema = (SchemaRecord)ScriptsMetaSchema.Root;
                return true;
            }
            schema = default!;
            return false;
        }
    }
}
