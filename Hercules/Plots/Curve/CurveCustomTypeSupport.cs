using Hercules.Documents;
using Hercules.Forms.Elements;
using Hercules.Forms.Schema;
using Hercules.Forms.Schema.Custom;
using Hercules.Shell;
using Json;
using System.Windows;

namespace Hercules.Plots
{
    public class CurveCustomTypeSupport : CustomTypeSupport
    {
        public const string TagValue = "curve";

        private readonly IDialogService dialogService;
        private readonly DocumentsModule documentsModule;

        public CurveCustomTypeSupport(FormSchema editorMetaSchema, IDialogService dialogService, DocumentsModule documentsModule)
            : base(editorMetaSchema)
        {
            this.dialogService = dialogService;
            this.documentsModule = documentsModule;
        }

        public override Element CreateElement(IContainer parent, CustomSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction, ElementFactoryContext context)
        {
            var curveType = (CurveSchemaType)type;
            return new CurveElement(parent, curveType, json, originalJson, transaction);
        }

        public override CustomSchemaType CreateSchemaType(IDocument editorDocument, SchemaType contentType, bool optional = false, string? help = null)
        {
            return new CurveSchemaType(contentType, editorDocument, documentsModule, dialogService, optional, help);
        }

        public override string Tag => TagValue;

        public override DocumentDraft CreateEditorDraft(string documentId, TempStorage tempStorage)
        {
            var curveEditor = new EditorCurve { DefaultViewport = new Rect(0, 0, 1, 1) };
            return new DocumentDraft(EditorCurveJsonSerializer.Instance.Serialize(curveEditor).AsObject);
        }
    }
}