using Hercules.Documents.Editor;
using Hercules.Shell;
using ICSharpCode.AvalonEdit.Document;
using System.Threading.Tasks;
using System.Windows.Input;
using Hercules.Forms.Elements;
using Json;

namespace Hercules.Scripting
{
    public class DocumentScriptTab : PageTab
    {
        public ForegroundJob Job { get; set; }
        public ICodeCompletionStrategy CodeCompletionStrategy { get; }
        public TextDocument Script { get; }

        public ICommand RunScriptCommand { get; }

        public Project Project { get; }
        public ScriptingModule ScriptingModule { get; }

        public SearchNotification Search { get; }

        private ISearchTarget? searchTarget;

        public ISearchTarget? SearchTarget
        {
            get => searchTarget;
            set => searchTarget = value;
        }

        public DocumentScriptTab(DocumentEditorPage page, Project project, ScriptingModule scriptingModule)
        {
            this.Project = project;
            this.ScriptingModule = scriptingModule;
            this.Job = new ForegroundJob();
            this.Title = "Script";
            this.CodeCompletionStrategy = new CodeCompletionStrategy(Project.SchemafulDatabase.SchemafulDocuments, Project.SchemafulDatabase.Schema);
            this.RunScriptCommand = Commands.ExecuteAsync(RunScriptJobAsync);
            var scriptElement = page.FormTab.Form.Root.GetByPath(new JsonPath("script")) as AvalonTextElement;
            this.Script = scriptElement?.Document;
            Search = new SearchNotification(RoutedCommands.FindNext);
            RoutedCommandBindings.Add(RoutedCommands.Find, Find);
            RoutedCommandBindings.Add(RoutedCommands.FindNext, FindNext, Search.HasContent);
            RoutedCommandBindings.Add(RoutedCommands.FindPrevious, FindPrevious, Search.HasContent);
        }

        Task RunScriptJobAsync()
        {
            ScriptingModule.SaveLastScript(Script.Text);
            return Job.Run("Run Script", (progress, ct) => ScriptingModule.RunScriptAsync(Script.Text, null, progress, ct));
        }

        void Find()
        {
            if (!Notifications.Items.Contains(Search))
                Notifications.Show(Search);
            Search.Activate();
        }

        void FindNext()
        {
            SearchTarget?.FindNext(Search.Text, SearchDirection.Next, Search.MatchCase, Search.WholeWord);
        }

        void FindPrevious()
        {
            SearchTarget?.FindNext(Search.Text, SearchDirection.Previous, Search.MatchCase, Search.WholeWord);
        }
    }
}