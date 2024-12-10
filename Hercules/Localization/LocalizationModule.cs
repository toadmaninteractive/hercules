using System.Windows.Input;
using Hercules.Documents;
using Hercules.Shell;

namespace Hercules.Localization
{
    public class LocalizationModule : CoreModule
    {
        public LocalizationModule(Core core) : base(core)
        {
            EditingCommand = Commands.Execute(Editing).If(() => Core.Project != null);
            var editingOption = new UiCommandOption("Text Editing", "fugue-globe", EditingCommand);
            Workspace.OptionManager.AddMenuOption(editingOption, "Data");
        }

        public ICommand EditingCommand { get; }

        private void Editing()
        {
            Core.Workspace.WindowService.OpenPage(new LocalizationEditingPage(Core.Project!, Core.GetModule<DocumentsModule>()));
        }
    }
}
