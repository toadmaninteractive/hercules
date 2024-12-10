using System.ComponentModel;

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
            this.Pane = "RightToolsPane";
            this.IsVisible = false;
            this.windowService = windowService;
            windowService.PropertyChanged += WindowService_PropertyChanged;
        }

        private void WindowService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IWindowService.ActiveContent))
            {
                if (windowService.ActiveContent != this)
                {
                    LayoutItem = windowService.ActiveContent;
                    Content = layoutItem?.Properties;
                }
            }
        }
    }
}