using Hercules.Forms.Elements;
using System;
using System.Collections.Generic;

namespace Hercules.Forms
{
    public interface IUndoHandler<T>
    {
        void Undo(T step, ITransaction transaction);

        void Redo(T step, ITransaction transaction);
    }

    public class UndoHistory<T> : NotifyPropertyChanged where T : class
    {
        readonly IUndoHandler<T> handler;
        readonly List<T> stack = new();
        private int index;

        public UndoHistory(IUndoHandler<T> handler)
        {
            this.handler = handler;
        }

        public void Reset()
        {
            stack.Clear();
            index = 0;
        }

        public bool CanUndo => index > 0;

        public bool CanRedo => stack.Count > index;

        public T? Head => index == 0 ? null : stack[index - 1];

        public void Push(T step)
        {
            if (index == stack.Count)
            {
                stack.Add(step);
                index++;
            }
            else
            {
                stack[index] = step;
                index++;
                if (stack.Count > index)
                    stack.RemoveRange(index, stack.Count - index);
            }
        }

        public void Undo(ITransaction transaction)
        {
            if (index == 0)
                throw new InvalidOperationException();
            index--;
            handler.Undo(stack[index], transaction);
        }

        public void Redo(ITransaction transaction)
        {
            if (stack.Count <= index)
                throw new InvalidOperationException();
            handler.Redo(stack[index], transaction);
            index++;
        }

        public void Clear()
        {
            stack.Clear();
            index = 0;
        }
    }
}