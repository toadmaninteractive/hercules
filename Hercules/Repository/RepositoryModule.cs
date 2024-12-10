using Hercules.Shell;

namespace Hercules.Repository
{
    public class RepositoryModule : CoreModule
    {
        public RepositoryModule(Core core)
            : base(core)
        {
            var projectSettingsCommand = Commands.Execute(() => Core.Project!.Settings.ShowDialog()).If(() => Core.Project != null);
            var projectSettingsOption = new UiCommandOption("Project Settings...", null, projectSettingsCommand);
            Workspace.OptionManager.AddMenuOption(projectSettingsOption, "Connection#10");
        }
    }
}
