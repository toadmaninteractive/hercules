using System;
using System.Collections.Generic;
using System.Linq;
using Telerik.Windows.Diagrams.Core;

namespace Hercules.Diagrams
{
    public class DisabledUndoRedoService : IUndoRedoService
    {
        public event EventHandler<CommandEventArgs>? ActionExecuted { add { } remove { } }

        public void AddCommand(ICommand command)
        {
        }

        public bool CanRedo()
        {
            return false;
        }

        public bool CanUndo()
        {
            return false;
        }

        public void Clear()
        {
        }

        public void ExecuteCommand(ICommand command, object? state = null)
        {
            command.Execute(state);
        }

        public bool IsActive => false;

        public void Redo()
        {
        }

        public int RedoBufferSize
        {
            get => 0;
            set { }
        }

        public IEnumerable<ICommand> RedoStack => Enumerable.Empty<ICommand>();

        public void RemoveCommand(ICommand command)
        {
        }

        public void Undo(object? state = null)
        {
        }

        public int UndoBufferSize
        {
            get => 0;
            set { }
        }

        public IEnumerable<ICommand> UndoStack => Enumerable.Empty<ICommand>();
    }
}
