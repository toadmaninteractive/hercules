using Hercules.Documents;
using Hercules.Forms.Elements;
using Hercules.Forms.Schema;
using Hercules.Forms.Schema.Custom;
using Hercules.Shell;
using Json;

namespace Hercules.InteractiveMaps
{
    public class InteractiveMapCustomTypeSupport : CustomTypeSupport
    {
        private readonly PropertyEditorTool propertyEditorTool;

        public InteractiveMapCustomTypeSupport(FormSchema editorMetaSchema, PropertyEditorTool propertyEditorTool)
            : base(editorMetaSchema)
        {
            this.propertyEditorTool = propertyEditorTool;
        }

        public const string TagValue = "interactive_map";

        public override Element CreateElement(IContainer parent, CustomSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction, ElementFactoryContext context)
        {
            var plotType = (InteractiveMapSchemaType)type;
            return new InteractiveMapElement(parent, plotType, json, originalJson, transaction, context.EditorPage, propertyEditorTool);
        }

        public override CustomSchemaType CreateSchemaType(IDocument editorDocument, SchemaType contentType, bool optional = false, string? help = null)
        {
            var editor = EditorInteractiveMapJsonSerializer.Instance.Deserialize(editorDocument.Json);
            return new InteractiveMapSchemaType(contentType, editor, optional, help);
        }

        public override string Tag => TagValue;

        public override DocumentDraft CreateEditorDraft(string documentId, TempStorage tempStorage)
        {
            var imapEditor = new EditorInteractiveMap { Title = "Interactive Map" };
            return new DocumentDraft(EditorInteractiveMapJsonSerializer.Instance.Serialize(imapEditor).AsObject);
        }
    }
}
