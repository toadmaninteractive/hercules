using System.Collections.ObjectModel;

namespace Hercules.Shell
{
    public abstract class TabbedPage : Page
    {
        public ObservableCollection<PageTab> Tabs { get; } = new();

        PageTab? activeTab;

        public PageTab? ActiveTab
        {
            get => activeTab;
            set
            {
                if (activeTab != value)
                {
                    activeTab?.OnDeactivate();
                    activeTab = value;
                    RaisePropertyChanged();
                    activeTab?.OnActivate();
                }
            }
        }

        protected override void OnClose()
        {
            activeTab?.OnDeactivate();
            foreach (var tab in Tabs)
                tab.OnClose();
        }
    }
}
