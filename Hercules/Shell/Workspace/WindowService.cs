using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;

namespace Hercules.Shell
{
    public class WindowService : NotifyPropertyChanged, IWindowService, IClosePageService
    {
        public ObservableRangeCollection<Page> Pages { get; } = new();
        public ObservableCollection<Tool> Tools { get; } = new();
        public ObservableCollection<Tool> LeftTools { get; } = new();
        public ObservableCollection<Tool> RightTools { get; } = new();
        public ObservableCollection<Tool> BottomTools { get; } = new();

        IReadOnlyList<Page> IWindowService.Pages => Pages;
        IReadOnlyList<Tool> IWindowService.Tools => Tools;

        private DockingLayoutItem? activeContent;

        public DockingLayoutItem? ActiveContent
        {
            get => activeContent;
            set
            {
                if (activeContent == value)
                    return;
                if (activeContent != null)
                    activeContent.IsActive = false;
                activeContent = value;
                if (value != null)
                    value.IsActive = true;
                if (value is Page page)
                    SelectedPage = page;
                RaisePropertyChanged();
            }
        }

        private Page? selectedPage;

        // Page shown in the document area; it is not the active content while a tool has focus
        public Page? SelectedPage
        {
            get => selectedPage;
            set
            {
                if (!SetField(ref selectedPage, value))
                    return;
                if (value != null)
                    ActiveContent = value;
                else if (ActiveContent is Page)
                    ActiveContent = null;
            }
        }

        private readonly Subject<Page> whenAddingPage = new Subject<Page>();
        public IObservable<Page> WhenAddingPage => whenAddingPage.AsObservable();

        public ICommand CloseAllPagesCommand { get; }
        public ICommand<Page> CloseAllButThisCommand { get; }

        public WindowService()
        {
            CloseAllPagesCommand = Commands.Execute(() => CloseAllPages(CloseDirtyPageAction.AskConfirmation)).If(() => Pages.Count > 0);
            CloseAllButThisCommand = Commands.Execute<Page>(page => CloseAllPages(CloseDirtyPageAction.AskConfirmation, p => p != page)).If(_ => Pages.Count > 1);
        }

        public void OpenPage(Page page)
        {
            AddPage(page);
            ActiveContent = page;
        }

        public void AddPage(Page page)
        {
            page.AssignClosePageService(this);
            whenAddingPage.OnNext(page);
            Pages.Add(page);
        }

        public void AddPages(IReadOnlyCollection<Page> pages)
        {
            foreach (var page in pages)
            {
                page.AssignClosePageService(this);
                whenAddingPage.OnNext(page);
            }
            Pages.AddRange(pages);
        }

        public void AddTool(Tool tool)
        {
            Tools.Add(tool);
            UpdateToolPanel(tool);
            tool.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(Tool.IsVisible))
                    UpdateToolPanel(tool);
            };
        }

        private void UpdateToolPanel(Tool tool)
        {
            var panel = tool.Location switch
            {
                ToolLocation.Left => LeftTools,
                ToolLocation.Right => RightTools,
                _ => BottomTools,
            };
            if (tool.IsVisible == panel.Contains(tool))
                return;
            if (tool.IsVisible)
            {
                // keep the registration order of tools in the panel
                var index = Tools.IndexOf(tool);
                panel.Insert(panel.Count(t => Tools.IndexOf(t) < index), tool);
            }
            else
            {
                panel.Remove(tool);
                if (ActiveContent == tool)
                    ActiveContent = SelectedPage;
            }
        }

        public T OpenSingletonPage<T>(Func<T> creator) where T : Page
        {
            var page = Pages.OfType<T>().FirstOrDefault();
            if (page == null)
            {
                page = creator();
                AddPage(page);
            }
            ActiveContent = page;
            return page;
        }

        public ClosePageContext GetClosePageContext(CloseDirtyPageAction closeDirtyPageAction, bool isBatch = false)
        {
            return new ClosePageContext(closeDirtyPageAction, isBatch, page => Pages.Remove(page));
        }

        public void CloseAllPages(CloseDirtyPageAction dirtyAction, Predicate<Page>? predicate = null)
        {
            var removedPages = new List<Page>();
            var context = new ClosePageContext(dirtyAction, true, page => removedPages.Add(page));
            foreach (var page in Pages.ToArray())
            {
                if (predicate == null || predicate(page))
                    page.Close(context);
            }

            Pages.RemoveRange(removedPages, NotifyCollectionChangedAction.Reset);
        }
    }
}
