using Hercules.Documents.Editor;
using System;
using System.Reactive.Linq;

namespace Hercules.Dialogs
{
    class DialogModule : CoreModule
    {
        public DialogModule(Core core)
            : base(core)
        {
            documentPageSubscription = Workspace.WindowService.WhenAddingPage.OfType<DocumentEditorPage>().Subscribe(page => page.Services.Add(new DialogPageService(page, Workspace.PropertyEditorTool)));
        }

        private readonly IDisposable documentPageSubscription;
    }
}