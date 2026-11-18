using System.Windows.Input;

namespace Hercules.Shell
{
    public enum ToolLocation
    {
        Left,
        Right,
        // under the Right panel, in the same column
        RightBottom,
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

        public bool IsVisible
        {
            get;
            set => SetField(ref field, value);
        }

        public bool IsSelected
        {
            get;
            set => SetField(ref field, value);
        }

        public void Show()
        {
            IsVisible = true;
            IsSelected = true;
        }
    }
}
