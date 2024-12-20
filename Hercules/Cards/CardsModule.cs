using Hercules.Documents;
using Hercules.Shell;
using System.Collections.Generic;

namespace Hercules.Cards
{
    public class CardsModule : CoreModule
    {
        public ICommand<Category> CardViewCommand { get; }
        private Dictionary<string, CardSettings> cardSettings;

        public CardsModule(Core core)
            : base(core)
        {
            this.CardViewCommand = Commands.Execute<Category>(CardView).If(_ => Core.Project != null);
            var cardViewOption = new UiCommandOption("Tile View...", Fugue.Icons.Layout4, CardViewCommand.ForContext(Workspace));
            Workspace.OptionManager.AddContextMenuOption<Category>(cardViewOption);
            cardSettings = new();
        }

        public void CardView(Category category)
        {
            var editor = new CardViewPage(Core.Project!, GetCardSettings(category.Name), Core.GetModule<DocumentsModule>(), category!, Core.Workspace.OptionManager);
            Core.Workspace.WindowService.OpenPage(editor);
        }

        public override void OnLoadProject(Project project, ISettingsReader settingsReader)
        {
            if (settingsReader.Read<Dictionary<string, CardSettings>>("Cards", out var cardSettings))
            {
                this.cardSettings = cardSettings;
            }
        }

        public override void OnCloseProject()
        {
            cardSettings.Clear();
        }

        public override void OnSaveProject(ISettingsWriter settingsWriter)
        {
            settingsWriter.Write("Cards", cardSettings);
        }

        public CardSettings GetCardSettings(string category)
        {
            if (!cardSettings.TryGetValue(category, out var value))
            {
                value = new CardSettings();
                cardSettings[category] = value;
            }
            return value;
        }
    }
}
