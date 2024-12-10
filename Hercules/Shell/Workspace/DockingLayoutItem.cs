using System;
using System.Windows.Input;

namespace Hercules.Shell
{
    public abstract class DockingLayoutItem : NotifyPropertyChanged, ICommandContext
    {
        public CommandBindingCollection RoutedCommandBindings { get; } = new CommandBindingCollection();

        string? title;

        public string? Title
        {
            get => title;
            set => SetField(ref title, value);
        }

        string? contentId;

        public string? ContentId
        {
            get => contentId;
            set => SetField(ref contentId, value);
        }

        bool isActive;

        public bool IsActive
        {
            get => isActive;
            set => SetField(ref isActive, value);
        }

        bool isSelected;

        public bool IsSelected
        {
            get => isSelected;
            set => SetField(ref isSelected, value);
        }

        string? status;

        public string? Status
        {
            get => status;
            set => SetField(ref status, value);
        }

        string? operation;

        public string? Operation
        {
            get => operation;
            set => SetField(ref operation, value);
        }

        public ObservableValue<object?> Properties { get; } = new ObservableValue<object?>(null);

        public virtual object? GetCommandParameter(Type type) => null;
    }
}
