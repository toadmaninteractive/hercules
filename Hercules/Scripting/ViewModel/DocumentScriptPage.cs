using Hercules.Documents;
using Hercules.Documents.Editor;
using Hercules.Shell;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TextDocument = ICSharpCode.AvalonEdit.Document.TextDocument;

namespace Hercules.Scripting
{
    public class DocumentScriptPage : Page
    {
        const string Example = "// Enter JavaScript script below.\n// This script will be applied to every document.\n// Use doc variable to access document object.\n// Use hercules variable to access Hercules API.\n// Example:\n//     if (doc.category==\"test\") doc.my_field=10;\n";

        public bool OpenedDocumentsOnly { get; set; }
        public ForegroundJob Job { get; set; }
        public ICodeCompletionStrategy CodeCompletionStrategy { get; }
        public ObservableCollection<Category> Categories { get; } = new ObservableCollection<Category>();
        public TextDocument Script { get; }
        public ICommand ClearCategoriesCommand { get; }

        public ICommand ImportJsonCommand { get; }
        public ICommand RunScriptCommand { get; }

        public ICommand SaveLocalScriptCommand { get; }

        public ICommand LoadLocalScriptCommand { get; }
        public Project Project { get; }
        public ScriptingModule ScriptingModule { get; }
        public Workspace Workspace { get; }

        bool forEachDocument = false;

        public bool ForEachDocument
        {
            get => forEachDocument;
            set => SetField(ref forEachDocument, value);
        }

        public SearchNotification Search { get; }

        private ISearchTarget? searchTarget;

        public ISearchTarget? SearchTarget
        {
            get => searchTarget;
            set => searchTarget = value;
        }

        public DocumentScriptPage(Project project, ScriptingModule scriptingModule, Workspace workspace, string? preset = null)
        {
            this.Project = project;
            this.ScriptingModule = scriptingModule;
            this.Workspace = workspace;
            this.Job = new ForegroundJob();
            this.Title = "Script";
            this.ContentId = "{Script}";
            this.CodeCompletionStrategy = new CodeCompletionStrategy(Project.SchemafulDatabase.SchemafulDocuments, Project.SchemafulDatabase.Schema);
            this.ClearCategoriesCommand = Commands.Execute(() => this.Categories.Clear());
            this.RunScriptCommand = Commands.ExecuteAsync(RunScriptJobAsync);
            this.Script = new TextDocument(preset ?? Example);
            this.ImportJsonCommand = Commands.Execute(ImportJson);
            this.SaveLocalScriptCommand = Commands.Execute(SaveLocalScript);
            this.LoadLocalScriptCommand = Commands.Execute(LoadLocalScript);
            if (!string.IsNullOrWhiteSpace(ScriptingModule.LastScript))
                this.Script.Text = ScriptingModule.LastScript;

            Search = new SearchNotification(RoutedCommands.FindNext);
            RoutedCommandBindings.Add(RoutedCommands.Find, Find);
            RoutedCommandBindings.Add(RoutedCommands.FindNext, FindNext, Search.HasContent);
            RoutedCommandBindings.Add(RoutedCommands.FindPrevious, FindPrevious, Search.HasContent);
        }

        private void LoadLocalScript()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".js",
                Filter = "JavaScript file (*.js)|*.js|All Files (*.*)|*.*",
                Title = "Load Script"
            };

            if (dlg.ShowDialog() == true)
            {
                Script.Text = System.IO.File.ReadAllText(dlg.FileName);
            }
        }

        private void SaveLocalScript()
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
            {
                DefaultExt = ".js",
                Filter = "JavaScript file (*.js)|*.js|All Files (*.*)|*.*",
                Title = "Save Script"
            };

            if (dlg.ShowDialog() == true)
            {
                System.IO.File.WriteAllText(dlg.FileName, Script.Text);
            }
        }

        Task RunScriptJobAsync()
        {
            ScriptingModule.SaveLastScript(Script.Text);
            List<IDocument>? docsList = null;
            if (ForEachDocument)
            {
                var docs = (Categories.Count > 0) ? Categories.SelectMany(cat => cat.Documents) : Project.Database.Documents.Values;
                if (OpenedDocumentsOnly)
                    docs = docs.Where(doc => Workspace.WindowService.Pages.OfType<DocumentEditorPage>().Any(vm => vm.Document.DocumentId == doc.DocumentId));
                docsList = docs.OrderBy(d => d.DocumentId).ToList();
            }
            return Job.Run("Run Script", (progress, ct) => ScriptingModule.RunScriptAsync(Script.Text, docsList, progress, ct));
        }

        private void ImportJson()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Import Json"
            };

            if (dlg.ShowDialog() == true)
            {
                var escapeFileName = dlg.FileName.Replace(@"\", @"\\", StringComparison.Ordinal);
                Script.Text = Script.Text + $@"{Environment.NewLine}var json = hercules.loadJsonFromFile(""{escapeFileName}"");{Environment.NewLine}";
            }
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