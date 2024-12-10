using Hercules.Documents;

namespace Hercules.Plots
{
    public class PlotsModule : CoreModule
    {
        public PlotsModule(Core core)
            : base(core)
        {
            var documentsModule = core.GetModule<DocumentsModule>();
            var customTypeRegistry = documentsModule.CustomTypeRegistry;
            var dialogService = core.Workspace.DialogService;
            customTypeRegistry.Register(new CurveCustomTypeSupport(documentsModule.EditorsMetaSchema, dialogService, documentsModule));
            customTypeRegistry.Register(new PlotCustomTypeSupport(documentsModule.EditorsMetaSchema, dialogService));
        }
    }
}