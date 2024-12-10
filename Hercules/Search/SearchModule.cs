using Hercules.Documents;
using Hercules.Shell;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace Hercules.Search
{
    public class SearchModule : CoreModule
    {
        public SearchModule(Core core)
            : base(core)
        {
            Workspace.WindowService.AddTool(SearchTool = new SearchTool(this, core.ProjectObservable));
            Workspace.WindowService.AddTool(SearchResultsTool = new SearchResultsTool((documentId, path) => Core.GetModule<DocumentsModule>().EditDocument(documentId, null, path)));

            FindReferencesCommand = Commands.Execute<IDocument>(doc => FindReferences(new[] { doc })).IfNotNull().AsBulk(FindReferences);
            var findReferencesGesture = new KeyGesture(Key.F6);
            var findReferencesOption = new UiCommandOption("Find References", null, FindReferencesCommand.ForContext(Workspace), findReferencesGesture);
            Workspace.OptionManager.AddMenuOption(findReferencesOption, "Document#10");
            Workspace.OptionManager.AddContextMenuOption<IDocument>(findReferencesOption);
            Workspace.AddGesture(findReferencesOption);

            var searchCommand = Commands.Execute(() => SearchTool.Show()).If(() => Core.Project != null);
            var searchGesture = new KeyGesture(Key.F, ModifierKeys.Control | ModifierKeys.Shift);
            var searchOption = new UiCommandOption("Search in Documents", Fugue.Icons.DocumentSearchResult, searchCommand, searchGesture);
            Workspace.OptionManager.AddMenuOption(searchOption, "Data#0", showInToolbar: true);
            Workspace.AddGesture(searchOption);
        }

        public IBulkCommand<IDocument> FindReferencesCommand { get; }
        public IBulkCommand<IDocument> ReferenceGraphCommand { get; }

        public SearchTool SearchTool { get; }
        public SearchResultsTool SearchResultsTool { get; }

        public void ShowSearchResults(SearchResults results)
        {
            SearchResultsTool.Results = results;
            SearchResultsTool.Show();
        }

        private void FindReferences(IReadOnlyCollection<IDocument> documents)
        {
            var results = KeySearchVisitor.Search(Core.Project!.SchemafulDatabase.Schema!, Core.Project.SchemafulDatabase.SchemafulDocuments, documents.Select(d => d.DocumentId).ToHashSet());
            SearchResultsTool.Results = results;
            SearchResultsTool.Show();
        }
    }
}
