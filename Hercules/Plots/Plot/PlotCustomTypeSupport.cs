using Hercules.Documents;
using Hercules.Forms.Elements;
using Hercules.Forms.Schema;
using Hercules.Forms.Schema.Custom;
using Hercules.Shell;
using Json;
using System.Windows;

namespace Hercules.Plots
{
    public class PlotCustomTypeSupport : CustomTypeSupport
    {
        public const string TagValue = "plot";

        private readonly IDialogService dialogService;

        public PlotCustomTypeSupport(FormSchema editorMetaSchema, IDialogService dialogService)
            : base(editorMetaSchema)
        {
            this.dialogService = dialogService;
        }

        public override Element CreateElement(IContainer parent, CustomSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction, ElementFactoryContext context)
        {
            var plotType = (PlotSchemaType)type;
            return new PlotElement(parent, plotType, json, originalJson, transaction);
        }

        public override CustomSchemaType CreateSchemaType(IDocument editorDocument, SchemaType contentType, bool optional = false, string? help = null)
        {
            return new PlotSchemaType(contentType, editorDocument, dialogService, optional, help);
        }

        public override string Tag => TagValue;

        public override DocumentDraft CreateEditorDraft(string documentId, TempStorage tempStorage)
        {
            var plotEditor = new EditorPlot { PlotType = PlotType.Points, DefaultViewport = new Rect(0, 0, 1, 1) };
            return new DocumentDraft(EditorPlotJsonSerializer.Instance.Serialize(plotEditor).AsObject);
        }
    }
}