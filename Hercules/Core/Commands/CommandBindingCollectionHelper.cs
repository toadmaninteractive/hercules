using System;
using System.Windows.Input;

namespace Hercules
{
    public static class CommandBindingCollectionHelper
    {
        extension(CommandBindingCollection commandBindings)
        {
            public void Add(RoutedCommand source, ICommand target)
            {
                ArgumentNullException.ThrowIfNull(commandBindings);
                ArgumentNullException.ThrowIfNull(source);
                ArgumentNullException.ThrowIfNull(target);

                var binding = new CommandBinding(
                    source,
                    (sender, args) =>
                    {
                        target.Execute(args.Parameter);
                        args.Handled = true;
                    },
                    (sender, args) =>
                    {
                        args.CanExecute = target.CanExecute(args.Parameter);
                        args.Handled = true;
                    });
                commandBindings.Add(binding);
            }

            public void Add(RoutedCommand source, Action execute)
            {
                ArgumentNullException.ThrowIfNull(commandBindings);
                ArgumentNullException.ThrowIfNull(source);
                ArgumentNullException.ThrowIfNull(execute);

                var binding = new CommandBinding(
                    source,
                    (sender, args) =>
                    {
                        execute();
                        args.Handled = true;
                    });
                commandBindings.Add(binding);
            }

            public void Add(RoutedCommand source, Action execute, Func<bool> canExecute)
            {
                ArgumentNullException.ThrowIfNull(commandBindings);
                ArgumentNullException.ThrowIfNull(source);
                ArgumentNullException.ThrowIfNull(execute);
                ArgumentNullException.ThrowIfNull(canExecute);

                var binding = new CommandBinding(
                    source,
                    (sender, args) =>
                    {
                        execute();
                        args.Handled = true;
                    },
                    (sender, args) =>
                    {
                        args.CanExecute = canExecute();
                        args.Handled = true;
                    });
                commandBindings.Add(binding);
            }

            public void Add<T>(RoutedCommand source, ICommand<T> target, ICommandContext context) where T : class
            {
                ArgumentNullException.ThrowIfNull(commandBindings);
                ArgumentNullException.ThrowIfNull(source);
                ArgumentNullException.ThrowIfNull(target);
                ArgumentNullException.ThrowIfNull(context);

                Add(commandBindings, source, target.ForContext(context));
            }
        }
    }
}
