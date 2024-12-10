using System;
using System.Collections.Generic;

namespace Hercules.CommandsImpl
{
    internal class BulkCommand<T> : IBulkCommand<T>
    {
        public ICommand<T> Single { get; }
        public ICommand<IReadOnlyCollection<T>> Bulk { get; }

        public BulkCommand(ICommand<T> singleCommand, ICommand<IReadOnlyCollection<T>> bulkCommand)
        {
            this.Single = singleCommand;
            this.Bulk = bulkCommand;
        }

        public event EventHandler? CanExecuteChanged
        {
            add
            {
                Single.CanExecuteChanged += value;
                Bulk.CanExecuteChanged += value;
            }
            remove
            {
                Single.CanExecuteChanged -= value;
                Bulk.CanExecuteChanged -= value;
            }
        }

        public bool CanExecute(object? parameter)
        {
            return parameter switch
            {
                T singleParam => Single.CanExecute(singleParam),
                IReadOnlyCollection<T> bulkParams => Bulk.CanExecute(bulkParams),
                _ => false
            };
        }

        public void Execute(object? parameter)
        {
            if (parameter is T singleParam)
                Single.Execute(singleParam);
            else if (parameter is IReadOnlyCollection<T> bulkParams)
                Bulk.Execute(bulkParams);
        }
    }
}
