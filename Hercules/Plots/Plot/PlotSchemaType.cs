using Hercules.Documents;
using Hercules.Forms.Schema;
using Hercules.Forms.Schema.Custom;
using Hercules.Shell;

namespace Hercules.Plots
{
    public class PlotSchemaType : CustomSchemaType
    {
        public IDocument EditorDocument { get; }
        public IDialogService DialogService { get; }

        public override string Tag => PlotCustomTypeSupport.TagValue;

        public PlotSchemaType(SchemaType contentType, IDocument editorDocument, IDialogService dialogService, bool optional = false, string? help = null)
            : base(contentType, optional, help)
        {
            EditorDocument = editorDocument;
            DialogService = dialogService;
        }

        public EditorPlot GetEditor()
        {
            return EditorPlotJsonSerializer.Instance.Deserialize(EditorDocument.Json);
        }
    }
}