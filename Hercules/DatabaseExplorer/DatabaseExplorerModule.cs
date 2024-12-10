using Hercules.Documents;
using Hercules.Shell;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace Hercules.DatabaseExplorer
{
    public class DatabaseExplorerModule : CoreModule
    {
        public DatabaseExplorerModule(Core core)
            : base(core)
        {
            Workspace.WindowService.AddTool(DatabaseExplorerTool = new DatabaseExplorerTool(core.ProjectObservable, core.GetModule<DocumentsModule>(), core.Workspace.OptionManager));

            var databaseExplorerOption = new UiCommandOption("Database Explorer", Fugue.Icons.DocumentTree, DatabaseExplorerTool.Show);
            Workspace.OptionManager.AddMenuOption(databaseExplorerOption, "View#10");

            var gotoExplorerGesture = new KeyGesture(Key.E, ModifierKeys.Control);
            var gotoExplorerCommand = Commands.Execute<IDocument>(GoToExplorer).ForContext(Workspace);
            Workspace.AddGesture(gotoExplorerCommand, gotoExplorerGesture);
        }

        private void GoToExplorer(IDocument document)
        {
            DatabaseExplorerTool.Show();
            DatabaseExplorerTool.Select(document);
        }

        public DatabaseExplorerTool DatabaseExplorerTool { get; }

        public override void OnLoadProject(Project project, ISettingsReader settingsReader)
        {
            if (settingsReader.Read<List<string>>("CollapsedCategories", out var collapsedCategories))
                foreach (var cat in collapsedCategories)
                    DatabaseExplorerTool.CollapsedCategories.Add(cat);
        }

        public override void OnSaveProject(ISettingsWriter settingsWriter)
        {
            settingsWriter.Write("CollapsedCategories", DatabaseExplorerTool.CollapsedCategories.ToList());
        }

        public override void OnCloseProject()
        {
            DatabaseExplorerTool.CollapsedCategories.Clear();
        }
    }
}
