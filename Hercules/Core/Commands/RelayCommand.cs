using System;
using System.Windows.Input;

namespace Hercules.CommandsImpl
{
    internal class RelayCommand<T> : ICommand<T>
    {
        public Action<T> ExecuteHandler { get; }
        public Predicate<T>? CanExecuteHandler { get; }

        public RelayCommand(Action<T> execute, Predicate<T>? canExecute = null)
        {
            this.ExecuteHandler = execute;
            this.CanExecuteHandler = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return CanExecuteHandler == null || CanExecuteHandler((T)parameter);
        }

        public bool CanExecute(T parameter)
        {
            return CanExecuteHandler == null || CanExecuteHandler(parameter);
        }

        public event EventHandler? CanExecuteChanged
        {
            add
            {
                if (CanExecuteHandler != null)
                    CommandManager.RequerySuggested += value;
            }
            remove
            {
                if (CanExecuteHandler != null)
                    CommandManager.RequerySuggested -= value;
            }
        }

        public void Execute(object parameter)
        {
            ExecuteHandler((T)parameter);
        }

        public void Execute(T parameter)
        {
            ExecuteHandler(parameter);
        }
    }

    internal class RelayCommand : ICommand
    {
        public Action ExecuteHandler { get; }
        public Func<bool>? CanExecuteHandler { get; }

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            this.ExecuteHandler = execute;
            this.CanExecuteHandler = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return CanExecuteHandler == null || CanExecuteHandler();
        }

        public event EventHandler? CanExecuteChanged
        {
            add
            {
                if (CanExecuteHandler != null)
                    CommandManager.RequerySuggested += value;
            }
            remove
            {
                if (CanExecuteHandler != null)
                    CommandManager.RequerySuggested -= value;
            }
        }

        public void Execute(object? parameter)
        {
            ExecuteHandler();
        }
    }

    internal class ParameterRelayCommand : ICommand
    {
        public ICommand HostCommand { get; }
        public Func<object?> Parameter { get; }

        public ParameterRelayCommand(ICommand hostCommand, Func<object?> parameter)
        {
            HostCommand = hostCommand;
            Parameter = parameter;
        }

        public bool CanExecute(object? parameter)
        {
            return HostCommand.CanExecute(Parameter());
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public void Execute(object? parameter)
        {
            HostCommand.Execute(Parameter());
        }
    }

    internal class ParameterRelayCommand<T> : ICommand where T : class
    {
        public ICommand<T> HostCommand { get; }
        public Func<T?> Parameter { get; }

        public ParameterRelayCommand(ICommand<T> hostCommand, Func<T?> parameter)
        {
            HostCommand = hostCommand;
            Parameter = parameter;
        }

        public bool CanExecute(object? parameter)
        {
            return HostCommand.CanExecute(Parameter());
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public void Execute(object? parameter)
        {
            HostCommand.Execute(Parameter());
        }
    }
}
