using Hercules.Documents;
using Hercules.Forms.Schema;
using Hercules.Forms.Schema.Custom;
using Hercules.Shell;

namespace Hercules.Plots
{
    public class CurveSchemaType : CustomSchemaType
    {
        public IDialogService DialogService { get; }
        public IDocument EditorDocument { get; }
        public DocumentsModule DocumentsModule { get; }

        public override string Tag => CurveCustomTypeSupport.TagValue;

        public CurveSchemaType(SchemaType contentType, IDocument editorDocument, DocumentsModule documentsModule, IDialogService dialogService, bool optional = false, string? help = null)
            : base(contentType, optional, help)
        {
            DialogService = dialogService;
            EditorDocument = editorDocument;
            DocumentsModule = documentsModule;
        }

        public void SaveEditor(EditorCurve editorCurve)
        {
            var draft = new DocumentDraft(EditorCurveJsonSerializer.Instance.Serialize(editorCurve).AsObject);
            DocumentsModule.SaveDocumentAsync(EditorDocument, draft).Track();
        }

        public void OpenEditorInNewTab(EditorCurve editorCurve)
        {
            DocumentsModule.EditDocument(EditorDocument.DocumentId, EditorCurveJsonSerializer.Instance.Serialize(editorCurve).AsObject);
        }

        public EditorCurve GetEditor()
        {
            return EditorCurveJsonSerializer.Instance.Deserialize(EditorDocument.Json);
        }
    }
}