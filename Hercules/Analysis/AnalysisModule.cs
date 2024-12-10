using Hercules.Documents;
using Hercules.Shell;
using System.Collections.Generic;
using System.Windows.Input;

namespace Hercules.Analysis
{
    public class AnalysisModule : CoreModule
    {
        public AnalysisModule(Core core)
            : base(core)
        {
            ReferenceGraphCommand = Commands.Execute<IDocument>(doc => ReferenceGraph(new[] { doc })).IfNotNull().AsBulk(ReferenceGraph);
            var referenceGraphOption = new UiCommandOption("Reference Graph", null, ReferenceGraphCommand.ForContext(Workspace));
            Workspace.OptionManager.AddMenuOption(referenceGraphOption, "Document#10");
            Workspace.OptionManager.AddContextMenuOption<IDocument>(referenceGraphOption);

            CategoryGraphCommand = Commands.Execute(CategoryGraph).If(() => Core.Project?.SchemafulDatabase != null);
            var categoryGraphOption = new UiCommandOption("Category Graph", null, CategoryGraphCommand);
            Workspace.OptionManager.AddMenuOption(categoryGraphOption, "Data");
        }

        public IBulkCommand<IDocument> ReferenceGraphCommand { get; }
        public ICommand CategoryGraphCommand { get; }

        private void ReferenceGraph(IReadOnlyCollection<IDocument> documents)
        {
            Core.Workspace.WindowService.OpenPage(new ReferenceGraphPage(Core.Project!, documents, Core.GetModule<DocumentsModule>().EditDocumentCommand));
        }

        private void CategoryGraph()
        {
            Core.Workspace.WindowService.OpenPage(new CategoryGraphPage(Core.Project!));
        }
    }
}
