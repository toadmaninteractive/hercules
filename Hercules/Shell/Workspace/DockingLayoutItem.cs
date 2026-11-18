using System;
using System.Windows.Input;

namespace Hercules.Shell
{
    public abstract class DockingLayoutItem : NotifyPropertyChanged, ICommandContext
    {
        public CommandBindingCollection RoutedCommandBindings { get; } = new CommandBindingCollection();

        public string? Title
        {
            get;
            set => SetField(ref field, value);
        }

        public string? ContentId
        {
            get;
            set => SetField(ref field, value);
        }

        public bool IsActive
        {
            get;
            set => SetField(ref field, value);
        }

        public string? Status
        {
            get;
            set => SetField(ref field, value);
        }

        public string? Operation
        {
            get;
            set => SetField(ref field, value);
        }

        public ObservableValue<object?> Properties { get; } = new ObservableValue<object?>(null);

        public virtual object? GetCommandParameter(Type type) => null;
    }
}
