using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;

namespace Hercules.Shell
{
    public interface IWindowService : INotifyPropertyChanged
    {
        IReadOnlyList<Page> Pages { get; }
        IReadOnlyList<Tool> Tools { get; }

        DockingLayoutItem? ActiveContent { get; set; }

        IObservable<Page> WhenAddingPage { get; }

        ICommand CloseAllPagesCommand { get; }

        void OpenPage(Page page);

        void AddPage(Page page);

        void AddPages(IReadOnlyCollection<Page> pages);

        T OpenSingletonPage<T>(Func<T> creator) where T : Page;

        void AddTool(Tool tool);

        void CloseAllPages(CloseDirtyPageAction dirtyAction, Predicate<Page>? predicate = null);
    }
}
