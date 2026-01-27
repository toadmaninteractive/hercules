using Json;
using System;
using System.Collections.Generic;

namespace Hercules.Forms.Elements
{
    public class Warning
    {
        public JsonPath Path { get; }
        public string Text { get; }

        public Warning(JsonPath path, string text)
        {
            Path = path;
            Text = text;
        }
    }

    public class UndoStep
    {
        public Action<ITransaction>? Undo { get; }
        public Action<ITransaction>? Redo { get; }

        public Action<ITransaction>? GroupUndo { get; private set; }
        public Action<ITransaction>? GroupRedo { get; private set; }
        public object? GroupKey { get; private set; }

        public bool RefreshPresentation { get; private set; }

        public void DoUndo(ITransaction transaction)
        {
            GroupUndo?.Invoke(transaction);
            Undo?.Invoke(transaction);
        }

        public void DoRedo(ITransaction transaction)
        {
            GroupRedo?.Invoke(transaction);
            Redo?.Invoke(transaction);
        }

        public void Append(Action<ITransaction>? groupUndo, Action<ITransaction>? groupRedo)
        {
            this.GroupRedo = groupRedo;
        }

        public void Seal()
        {
            this.GroupKey = null;
        }

        public UndoStep(Action<ITransaction>? undo, Action<ITransaction>? redo, object? groupKey, Action<ITransaction>? groupUndo, Action<ITransaction>? groupRedo, bool refreshPresentation)
        {
            this.Undo = undo;
            this.Redo = redo;
            this.GroupKey = groupKey;
            this.GroupUndo = groupUndo;
            this.GroupRedo = groupRedo;
            this.RefreshPresentation = refreshPresentation;
        }
    }

    public interface ITransaction
    {
        int TransactionId { get; }
        Element? UserInputElement { get; }
        bool IsFullInvalidationRequested { get; }
        bool ShouldRefreshPresentation { get; }
        IReadOnlyList<Warning> Warnings { get; }

        event Action? OnPostUpdate;

        void AddUndoRedo(Action<ITransaction> undo, Action<ITransaction> redo, object? undoRedoGroup = null);
        void AddWarning(JsonPath path, string text);
        void PostUpdate();
        void RequestFullInvalidation();
        void RefreshPresentation();
        Element? ExpandIntoView { get; set; }
    }

    public class UndoRedoTransaction : ITransaction
    {
        public int TransactionId { get; }

        public void RequestFullInvalidation() { }

        public Element? UserInputElement => null;

        public bool IsFullInvalidationRequested => true;

        public UndoRedoTransaction(int transactionId)
        {
            TransactionId = transactionId;
        }

        public event Action? OnPostUpdate;

        public void PostUpdate()
        {
            OnPostUpdate?.Invoke();
        }

        public void RefreshPresentation()
        {
            ShouldRefreshPresentation = true;
        }

        public void AddUndoRedo(Action<ITransaction> undo, Action<ITransaction> redo, object? undoRedoGroup)
        {
        }

        public void AddWarning(JsonPath path, string text)
        {
        }

        public IReadOnlyList<Warning> Warnings => Array.Empty<Warning>();

        public bool ShouldRefreshPresentation { get; private set; }
        public Element? ExpandIntoView { get; set; }
    }

    public class Transaction : ITransaction
    {
        public int TransactionId { get; }

        public Element? UserInputElement { get; }

        public bool HasUndoRedo { get; private set; }

        public void RefreshPresentation()
        {
            ShouldRefreshPresentation = true;
        }

        public bool ShouldRefreshPresentation { get; private set; }

        public Action<ITransaction>? Undo { get; private set; }
        public Action<ITransaction>? Redo { get; private set; }

        public Action<ITransaction>? GroupUndo { get; private set; }
        public Action<ITransaction>? GroupRedo { get; private set; }
        public object? GroupKey { get; private set; }

        public event Action? OnPostUpdate;

        public void RequestFullInvalidation() => IsFullInvalidationRequested = true;

        public bool IsFullInvalidationRequested { get; private set; }

        public IReadOnlyList<Warning> Warnings => warnings;

        private readonly List<Warning> warnings = new List<Warning>();
        public Element? ExpandIntoView { get; set; }

        public void AddWarning(JsonPath path, string text)
        {
            warnings.Add(new Warning(path, text));
        }

        public void AddUndoRedo(Action<ITransaction> undo, Action<ITransaction> redo, object? undoRedoGroup = null)
        {
            HasUndoRedo = true;
            if (undoRedoGroup == null)
            {
                Undo = undo + Undo;
                Redo += redo;
            }
            else
            {
                GroupUndo += undo;
                GroupRedo += redo;
                GroupKey = undoRedoGroup;
            }
        }

        public Transaction(int transactionId, Element? userInputElement)
        {
            TransactionId = transactionId;
            UserInputElement = userInputElement;
        }

        public void PostUpdate()
        {
            OnPostUpdate?.Invoke();
        }
    }

    public static class Transactions
    {
        private static int LastTransactionId = 0;

        public static Transaction Create(Element? userInputElement = null)
        {
            LastTransactionId++;
            return new Transaction(LastTransactionId, userInputElement);
        }

        public static ITransaction CreateUndoRedoTransaction()
        {
            LastTransactionId++;
            return new UndoRedoTransaction(LastTransactionId);
        }
    }
}
