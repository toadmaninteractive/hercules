using System.Windows.Input;

namespace Hercules.Shell
{
    public enum ToolLocation
    {
        Left,
        Right,
        Bottom,
    }

    public abstract class Tool : DockingLayoutItem
    {
        protected Tool()
        {
            CloseCommand = Commands.Execute(() => IsVisible = false);
        }

        public ToolLocation Location { get; protected init; }

        public ICommand CloseCommand { get; }

        bool isVisible;

        public bool IsVisible
        {
            get => isVisible;
            set => SetField(ref isVisible, value);
        }

        bool isSelected;

        public bool IsSelected
        {
            get => isSelected;
            set => SetField(ref isSelected, value);
        }

        public void Show()
        {
            IsVisible = true;
            IsSelected = true;
        }
    }
}
