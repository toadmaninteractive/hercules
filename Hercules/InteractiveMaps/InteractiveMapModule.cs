using Hercules.Documents;
using Hercules.Documents.Editor;
using System;
using System.Reactive.Linq;

namespace Hercules.InteractiveMaps
{
    public class InteractiveMapModule : CoreModule
    {
        public InteractiveMapModule(Core core)
            : base(core)
        {
            documentPageSubscription = Workspace.WindowService.WhenAddingPage.OfType<DocumentEditorPage>().Subscribe(page => page.Services.Add(new InteractiveMapPageService(page)));

            var documentsModule = core.GetModule<DocumentsModule>();
            documentsModule.CustomTypeRegistry.Register(new InteractiveMapCustomTypeSupport(documentsModule.EditorsMetaSchema, Workspace.PropertyEditorTool));
        }

        private readonly IDisposable documentPageSubscription;
    }
}