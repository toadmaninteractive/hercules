using System;
using System.Windows.Input;

namespace Hercules
{
    public class ToggleCommand : ICommand
    {
        public IObservableValue<bool> Source { get; }

        public ToggleCommand(IObservableValue<bool> source)
        {
            this.Source = source;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            Source.Value = !Source.Value;
        }
    }
}
