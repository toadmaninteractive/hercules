using Hercules.CommandsImpl;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Hercules
{
    public static class Commands
    {
        public static ICommand Execute(Action action)
        {
            return new RelayCommand(action);
        }

        public static ICommand ExecuteAsync(Func<Task> action)
        {
            return new RelayCommand(() => action().Track());
        }

        public static ICommand<T> Execute<T>(Action<T> action)
        {
            return new RelayCommand<T>(action);
        }

        public static ICommand<T> ExecuteAsync<T>(Func<T, Task> action)
        {
            return new RelayCommand<T>(p => action(p).Track());
        }

        public static IBulkCommand<T> Bulk<T>(Action<T> action, Action<IReadOnlyCollection<T>> bulkAction)
        {
            return new BulkCommand<T>(Execute(action), Execute(bulkAction));
        }

        public static IBulkCommand<T> BulkAsync<T>(Func<T, Task> action, Func<IReadOnlyCollection<T>, Task> bulkAction)
        {
            return new BulkCommand<T>(ExecuteAsync(action), ExecuteAsync(bulkAction).If(l => l != null && l.Count > 0));
        }
    }

    public static class CommandHelper
    {
        public static ICommand If(this ICommand command, Func<bool> condition)
        {
            ArgumentNullException.ThrowIfNull(nameof(command));

            if (command is RelayCommand relay)
            {
                if (relay.CanExecuteHandler == null)
                    return new RelayCommand(relay.ExecuteHandler, condition);
                else
                    return new RelayCommand(relay.ExecuteHandler, () => relay.CanExecuteHandler() && condition());
            }
            else
                return new RelayCommand<object>(command.Execute, p => command.CanExecute(p) && condition());
        }

        public static ICommand<T> If<T>(this ICommand<T> command, Predicate<T> condition)
        {
            ArgumentNullException.ThrowIfNull(nameof(command));

            if (command is RelayCommand<T> relay)
            {
                if (relay.CanExecuteHandler == null)
                    return new RelayCommand<T>(relay.ExecuteHandler, condition);
                else
                    return new RelayCommand<T>(relay.ExecuteHandler, p => relay.CanExecuteHandler(p) && condition(p));
            }
            else
                return new RelayCommand<T>(command.Execute, p => command.CanExecute(p) && condition(p));
        }

        public static ICommand<T> If<T>(this ICommand<T> command, Func<bool> condition)
        {
            ArgumentNullException.ThrowIfNull(nameof(command));

            if (command is RelayCommand<T> relay)
            {
                if (relay.CanExecuteHandler == null)
                    return new RelayCommand<T>(relay.ExecuteHandler, _ => condition());
                else
                    return new RelayCommand<T>(relay.ExecuteHandler, p => relay.CanExecuteHandler(p) && condition());
            }
            else
                return new RelayCommand<T>(command.Execute, p => command.CanExecute(p) && condition());
        }

        public static ICommand<T> IfNotNull<T>(this ICommand<T> command) where T : class
        {
            ArgumentNullException.ThrowIfNull(nameof(command));

            if (command is RelayCommand<T> relay)
            {
                if (relay.CanExecuteHandler == null)
                    return new RelayCommand<T>(relay.ExecuteHandler, p => p != null);
                else
                    return new RelayCommand<T>(relay.ExecuteHandler, p => p != null && relay.CanExecuteHandler(p));
            }
            else
                return new RelayCommand<T>(command.Execute, p => p != null && command.CanExecute(p));
        }

        public static ICommand For<T>(this ICommand<T> command, T parameter) where T : class
        {
            return new ParameterRelayCommand<T>(command, () => parameter);
        }

        public static ICommand For<T>(this ICommand<T> command, Func<T?> parameter) where T : class
        {
            return new ParameterRelayCommand<T>(command, parameter);
        }

        public static ICommand ForContext<T>(this ICommand<T> command, ICommandContext context) where T : class
        {
            return new ParameterRelayCommand<T>(command, () => context.GetCommandParameter(typeof(T)) as T);
        }

        public static ICommand ForContext<T>(this IBulkCommand<T> command, ICommandContext context)
        {
            return new ParameterRelayCommand(command, () => GetBulkCommandParameter<T>(context));
        }

        static object? GetBulkCommandParameter<T>(ICommandContext context)
        {
            return context.GetCommandParameter(typeof(IReadOnlyCollection<T>)) ?? context.GetCommandParameter(typeof(T));
        }

        public static IBulkCommand<T> AsBulk<T>(this ICommand<T> command)
        {
            return AsBulk(command, list => { foreach (var item in list) command.Execute(item); });
        }

        public static IBulkCommand<T> AsBulk<T>(this ICommand<T> command, Action<IReadOnlyCollection<T>> bulkExecute)
        {
            return new BulkCommand<T>(command, new RelayCommand<IReadOnlyCollection<T>>(bulkExecute, list => list != null && list.Count > 0));
        }
    }
}
