using Hercules.Scripting;
using Hercules.Shell;

namespace Hercules.Import
{
    public class ImportModule : CoreModule
    {
        public ImportModule(Core core)
            : base(core)
        {
            var importTableCommand = Commands.Execute(OpenImportTablePage).If(() => Core.Project?.SchemafulDatabase.Schema != null);
            var importTableOption = new UiCommandOption("Import Table", Fugue.Icons.TableImport, importTableCommand);
            Workspace.OptionManager.AddMenuOption(importTableOption, "Data#0", showInToolbar: true);
        }

        private void OpenImportTablePage()
        {
            Workspace.WindowService.OpenPage(new ImportTablePage(Core.Project!, Core.GetModule<ScriptingModule>()));
        }
    }
}
