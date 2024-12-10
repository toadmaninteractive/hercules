namespace Hercules.Shell
{
    public abstract class Tool : DockingLayoutItem
    {
        string? pane;

        public string? Pane
        {
            get => pane;
            set => SetField(ref pane, value);
        }

        bool isVisible;

        public bool IsVisible
        {
            get => isVisible;
            set => SetField(ref isVisible, value);
        }

        public void Show()
        {
            IsVisible = true;
            IsSelected = true;
            IsActive = true;
        }
    }
}
