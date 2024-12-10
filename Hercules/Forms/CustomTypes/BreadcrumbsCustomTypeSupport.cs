using Hercules.Documents;
using Hercules.Forms.Elements;
using Hercules.Forms.Schema;
using Hercules.Forms.Schema.Custom;
using Json;

namespace Hercules.Forms
{
    public class BreadcrumbsCustomTypeSupport : CustomTypeSupport
    {
        public const string TagValue = "breadcrumbs";

        public BreadcrumbsCustomTypeSupport(FormSchema editorMetaSchema)
            : base(editorMetaSchema)
        {
        }

        public override string Tag => TagValue;

        public override DocumentDraft CreateEditorDraft(string documentId, TempStorage tempStorage)
        {
            var editor = new EditorBreadcrumbs();
            return new DocumentDraft(EditorBreadcrumbsJsonSerializer.Instance.Serialize(editor).AsObject);
        }

        public override Element CreateElement(IContainer parent, CustomSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction, ElementFactoryContext context)
        {
            var schemaType = (BreadcrumbsSchemaType)type;
            return new BreadcrumbsElement(parent, schemaType, json, originalJson, transaction);
        }

        public override CustomSchemaType CreateSchemaType(IDocument editorDocument, SchemaType contentType, bool optional = false, string? help = null)
        {
            var editor = EditorBreadcrumbsJsonSerializer.Instance.Deserialize(editorDocument.Json);
            return new BreadcrumbsSchemaType(contentType, editor, optional, help);
        }
    }
}