using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;

namespace Hercules
{
    public interface ICommand<in T> : ICommand
    {
        bool CanExecute([AllowNull] T parameter);

        void Execute([AllowNull] T parameter);
    }

    public interface IBulkCommand<in T> : ICommand
    {
        ICommand<T> Single { get; }
        ICommand<IReadOnlyCollection<T>> Bulk { get; }
    }
}
