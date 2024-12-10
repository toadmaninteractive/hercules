using Hercules.Documents;
using Hercules.Shell;

namespace Hercules.Cards
{
    public class CardsModule : CoreModule
    {
        public ICommand<Category> CardViewCommand { get; }

        public CardsModule(Core core)
            : base(core)
        {
            this.CardViewCommand = Commands.Execute<Category>(CardView).If(_ => Core.Project != null);
            var cardViewOption = new UiCommandOption("Tile View...", Fugue.Icons.Layout4, CardViewCommand.ForContext(Workspace));
            Workspace.OptionManager.AddContextMenuOption<Category>(cardViewOption);
        }

        public void CardView(Category category)
        {
            var editor = new CardViewPage(Core.Project!, Core.GetModule<DocumentsModule>(), category!, Core.Workspace.OptionManager);
            Core.Workspace.WindowService.OpenPage(editor);
        }
    }
}
