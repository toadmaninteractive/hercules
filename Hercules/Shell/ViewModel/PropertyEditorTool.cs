using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Hercules.Shell
{
    public class PropertyEditorTool : Tool
    {
        private IReadOnlyObservableValue<object?>? content;

        public IReadOnlyObservableValue<object?>? Content
        {
            get => content;
            private set => SetField(ref content, value);
        }

        private DockingLayoutItem? layoutItem;

        public DockingLayoutItem? LayoutItem
        {
            get => layoutItem;
            private set => SetField(ref layoutItem, value);
        }

        private readonly IWindowService windowService;

        public PropertyEditorTool(IWindowService windowService)
        {
            this.Title = "Property Editor";
            this.ContentId = "{PropertyEditor}";
            this.Location = ToolLocation.Right;
            this.IsVisible = false;
            this.windowService = windowService;
            windowService.PropertyChanged += WindowService_PropertyChanged;
            ((INotifyCollectionChanged)windowService.Pages).CollectionChanged += Pages_CollectionChanged;
        }

        // Only pages have properties, so the editor keeps showing the last active page while a tool has focus
        private void WindowService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IWindowService.ActiveContent) && windowService.ActiveContent is Page or null)
                Show(windowService.ActiveContent);
        }

        // The shown page can be closed while a tool has focus, which doesn't change the active content
        private void Pages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (layoutItem is Page page && !windowService.Pages.Contains(page))
                Show(windowService.ActiveContent as Page);
        }

        private void Show(DockingLayoutItem? item)
        {
            LayoutItem = item;
            Content = item?.Properties;
        }
    }
}