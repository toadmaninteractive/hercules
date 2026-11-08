using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;

namespace Hercules.Shell
{
    public class WindowService : NotifyPropertyChanged, IWindowService, IClosePageService
    {
        // Pages of both editor groups
        public ObservableCollection<Page> Pages { get; } = new();
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
                {
                    var group = GetGroup(page);
                    ActiveGroup = group;
                    group.SelectedPage = page;
                }
                RaisePropertyChanged();
            }
        }

        public EditorGroup MainGroup { get; } = new();

        // Shown to the right of the main group while it has pages; it is never the only non-empty group
        public EditorGroup SideGroup { get; } = new();

        private EditorGroup activeGroup;

        // Group that new pages open in; it is not the active content's group while a tool has focus
        public EditorGroup ActiveGroup
        {
            get => activeGroup;
            private set => SetField(ref activeGroup, value);
        }

        private readonly Subject<Page> whenAddingPage = new Subject<Page>();
        public IObservable<Page> WhenAddingPage => whenAddingPage.AsObservable();

        public ICommand CloseAllPagesCommand { get; }
        public ICommand<Page> CloseAllButThisCommand { get; }
        public ICommand<Page> MoveToOtherGroupCommand { get; }

        public WindowService()
        {
            activeGroup = MainGroup;
            MainGroup.PropertyChanged += (_, e) => OnGroupPropertyChanged(MainGroup, e.PropertyName);
            SideGroup.PropertyChanged += (_, e) => OnGroupPropertyChanged(SideGroup, e.PropertyName);
            CloseAllPagesCommand = Commands.Execute(() => CloseAllPages(CloseDirtyPageAction.AskConfirmation)).If(() => Pages.Count > 0);
            CloseAllButThisCommand = Commands.Execute<Page>(page => CloseAllPages(CloseDirtyPageAction.AskConfirmation, p => p != page)).If(_ => Pages.Count > 1);
            MoveToOtherGroupCommand = Commands.Execute<Page>(MoveToOtherGroup).If(page => page != null && CanMovePage(page, GetOtherGroup(GetGroup(page))));
        }

        public EditorGroup GetGroup(Page page) => SideGroup.Pages.Contains(page) ? SideGroup : MainGroup;

        private EditorGroup GetOtherGroup(EditorGroup group) => group == MainGroup ? SideGroup : MainGroup;

        // Selection changes of the active group (clicking a tab, closing the selected page) change the active content;
        // the other group gets activated by keyboard focus or by setting ActiveContent
        private void OnGroupPropertyChanged(EditorGroup group, string? propertyName)
        {
            if (propertyName != nameof(EditorGroup.SelectedPage) || group != ActiveGroup)
                return;
            if (group.SelectedPage != null)
            {
                ActiveContent = group.SelectedPage;
            }
            else if (ActiveContent is Page)
            {
                ActiveGroup = GetOtherGroup(group);
                ActiveContent = ActiveGroup.SelectedPage;
            }
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
            ActiveGroup.Pages.Add(page);
        }

        // Pages are added one by one: TabControl doesn't support range notifications
        public void AddPages(IReadOnlyCollection<Page> pages)
        {
            foreach (var page in pages)
                AddPage(page);
        }

        public void AddTool(Tool tool)
        {
            Tools.Add(tool);
            UpdateToolPanel(tool);
            tool.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(Tool.IsVisible))
                    UpdateToolPanel(tool);
                else if (e.PropertyName == nameof(Tool.IsSelected) && tool.IsSelected)
                    SelectTool(tool);
            };
        }

        private ObservableCollection<Tool> GetToolPanel(Tool tool) => tool.Location switch
        {
            ToolLocation.Left => LeftTools,
            ToolLocation.Right => RightTools,
            _ => BottomTools,
        };

        // A non-empty panel always has exactly one selected tool; the panel's TabControl only follows it
        private void SelectTool(Tool tool)
        {
            foreach (var other in GetToolPanel(tool))
            {
                if (other != tool)
                    other.IsSelected = false;
            }
        }

        private void UpdateToolPanel(Tool tool)
        {
            var panel = GetToolPanel(tool);
            if (tool.IsVisible == panel.Contains(tool))
                return;
            if (tool.IsVisible)
            {
                // keep the registration order of tools in the panel
                var index = Tools.IndexOf(tool);
                panel.Insert(panel.Count(t => Tools.IndexOf(t) < index), tool);
                tool.IsSelected = panel.All(t => t == tool || !t.IsSelected);
            }
            else
            {
                panel.Remove(tool);
                if (tool.IsSelected && panel.Count > 0)
                    panel[0].IsSelected = true;
                tool.IsSelected = false;
                if (ActiveContent == tool)
                    ActiveContent = ActiveGroup.SelectedPage;
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
            return new ClosePageContext(closeDirtyPageAction, isBatch, RemovePage);
        }

        private void RemovePage(Page page)
        {
            GetGroup(page).Pages.Remove(page);
            Pages.Remove(page);
            CollapseEmptyGroups();
        }

        public void MoveToOtherGroup(Page page)
        {
            var group = GetOtherGroup(GetGroup(page));
            MovePage(page, group, group.Pages.Count);
        }

        // Moving the only page of the main group to the side group would just swap the groups
        public bool CanMovePage(Page page, EditorGroup group) => GetGroup(page) == group || group == MainGroup || MainGroup.Pages.Count > 1;

        // Moves the page to the index in the group, within the group or from the other group
        public void MovePage(Page page, EditorGroup group, int index)
        {
            var oldGroup = GetGroup(page);
            if (oldGroup == group)
            {
                group.Pages.Move(group.Pages.IndexOf(page), index);
            }
            else
            {
                oldGroup.Pages.Remove(page);
                group.Pages.Insert(index, page);
            }
            ActiveContent = page;
            CollapseEmptyGroups();
        }

        // Like VS Code closing an empty group: the side group's pages move to the main group,
        // and an empty side group is never active, so that new pages don't open in it
        private void CollapseEmptyGroups()
        {
            if (MainGroup.Pages.Count == 0 && SideGroup.Pages.Count > 0)
            {
                var selectedPage = SideGroup.SelectedPage;
                var pages = SideGroup.Pages.ToList();
                SideGroup.Pages.Clear();
                foreach (var page in pages)
                    MainGroup.Pages.Add(page);
                ActiveGroup = MainGroup;
                MainGroup.SelectedPage = selectedPage;
            }
            if (SideGroup.Pages.Count == 0)
                ActiveGroup = MainGroup;
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

            RemovePages(removedPages);
        }

        private void RemovePages(List<Page> removedPages)
        {
            // the inactive group first, so that the active group decides the active content
            RemovePages(GetOtherGroup(ActiveGroup), removedPages);
            RemovePages(ActiveGroup, removedPages);
            foreach (var page in removedPages)
                Pages.Remove(page);
            CollapseEmptyGroups();
        }

        // Removing the selected tab makes TabControl select and build a neighbour, so the selection moves
        // to a remaining page once, before the removal; a Reset would drop the cached views of remaining pages
        private static void RemovePages(EditorGroup group, List<Page> removedPages)
        {
            var remainingPage = group.Pages.FirstOrDefault(p => !removedPages.Contains(p));
            if (remainingPage == null)
            {
                group.Pages.Clear();
                return;
            }
            if (group.SelectedPage != null && removedPages.Contains(group.SelectedPage))
                group.SelectedPage = remainingPage;
            foreach (var page in removedPages)
                group.Pages.Remove(page);
        }
    }
}
