using Hercules.Documents;
using Hercules.Shell;
using Json;

namespace Hercules.Summary
{
    public class SummaryModule : CoreModule
    {
        public ICommand<Category> SummaryTableCommand { get; }

        public SummaryModule(Core core)
            : base(core)
        {
            this.SummaryTableCommand = Commands.Execute<Category>(SummaryTable).If(_ => Core.Project?.SchemafulDatabase?.Schema != null);

            var summaryTableOption = new UiCommandOption("Summary Table...", Fugue.Icons.Table, SummaryTableCommand.ForContext(Workspace));
            Workspace.OptionManager.AddMenuOption(summaryTableOption, "Data#0", showInToolbar: true);
            Workspace.OptionManager.AddContextMenuOption<Category>(summaryTableOption);
        }

        public void SummaryTable(Category category, JsonPath? selectedFieldPath)
        {
            var dialog = new SummaryParamsDialog(Core.Project!.SchemafulDatabase, category, null);

            if (selectedFieldPath != null)
            {
                StructureItem? rootItem = dialog.Structure!.GetByPath(selectedFieldPath);
                if (rootItem != null)
                    SelectNestedItems(rootItem);
            }

            ShowSummaryTable(dialog);
        }

        void SummaryTable(Category category)
        {
            var dialog = new SummaryParamsDialog(Core.Project!.SchemafulDatabase, category, null);
            ShowSummaryTable(dialog);
        }

        void ShowSummaryTable(SummaryParamsDialog dialog)
        {
            if (Workspace.DialogService.ShowDialog(dialog))
            {
                var editor = new DocumentSummaryPage(Core.Project, Core.GetModule<DocumentsModule>(), Core.Workspace.DialogService, Core.Workspace.SpreadsheetSettings, dialog.Structure!, dialog.Category!);
                Core.Workspace.WindowService.OpenPage(editor);
            }
        }

        private void SelectNestedItems(StructureItem rootField, bool recursive = true)
        {
            if (rootField is StructureValue structureValue)
            {
                structureValue.IsChecked = true;
            }
            else if (recursive && rootField is StructureCategory structureCategory)
            {
                structureCategory.IsExpanded = true;
                foreach (var item in structureCategory.Children)
                    SelectNestedItems(item, false);
            }
        }
    }
}
