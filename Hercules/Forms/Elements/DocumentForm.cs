using Hercules.Forms.Schema;
using Json;
using System;
using System.Collections.Generic;

namespace Hercules.Forms.Elements
{
    public enum TransactionStage
    {
        ContentUpdateFinalization,
        ExternalValidation,
        Cleanup,
    }

    public interface IFormService
    {
        void ProcessTransaction(ITransaction transaction, TransactionStage stage);
    }

    public sealed class DocumentForm : NotifyPropertyChanged, IContainer, IUndoHandler<UndoStep>
    {
        public RootElement Root { get; private set; } = default!;
        public UndoHistory<UndoStep> History { get; }

        public FormSettings Settings { get; }
        public IElementFactory Factory { get; }

        private bool isOptionFieldsVisible = true;

        public bool IsOptionFieldsVisible
        {
            get => isOptionFieldsVisible;
            set
            {
                if (SetField(ref isOptionFieldsVisible, value))
                    RefreshPresentation();
            }
        }

        DateTime lastAction;

        private readonly ObservableValue<bool> isModified = new ObservableValue<bool>(false);
        public IReadOnlyObservableValue<bool> IsModified => isModified;

        private readonly Dictionary<Type, IFormService> services = new Dictionary<Type, IFormService>();

        private ITransaction? currentTransaction;

        public event Action<ITransaction>? OnTransactionStarted;
        public event Action<ITransaction>? OnTransactionFinished;
        public event Action? OnRefreshPresentation;
        public event Action<Element>? OnExpandIntoView;

        public DocumentForm(ImmutableJsonObject data, ImmutableJson? originalJson, SchemaRecord record, IElementFactory factory, FormSettings settings)
        {
            this.Factory = factory;
            this.History = new UndoHistory<UndoStep>(this);
            this.Settings = settings;

            Run(transaction =>
            {
                transaction.RequestFullInvalidation();
                transaction.RefreshPresentation();
                Root = new RootElement(this, record, data, originalJson, transaction);
            });
        }

        public void Setup(ImmutableJsonObject data, ImmutableJson? originalJson, SchemaRecord record)
        {
            services.Clear();
            History.Clear();
            Run(transaction =>
            {
                transaction.RequestFullInvalidation();
                transaction.RefreshPresentation();
                Root = new RootElement(this, record, data, originalJson, transaction);
            });
            lastAction = default;
        }

        public void Override(ImmutableJsonObject baseJson)
        {
            Root.Inherit(baseJson);
        }

        public ImmutableJsonObject Json => Root.Json.AsObject;

        public void Undo()
        {
            History.Undo(Transactions.CreateUndoRedoTransaction());
        }

        public void Redo()
        {
            History.Redo(Transactions.CreateUndoRedoTransaction());
        }

        public Element ElementByPath(JsonPath path)
        {
            return Root.GetByPath(path);
        }

        public void Run(Action<ITransaction> fun, Element? userInputElement = null)
        {
            if (currentTransaction != null)
            {
                fun(currentTransaction);
            }
            else
            {
                var transaction = Transactions.Create(userInputElement);
                ExecuteTransaction(transaction, fun);
            }
        }

        void IContainer.ChildChanged(ITransaction transaction)
        {
        }

        bool IContainer.IsJsonKeyChild(Element child) => false;

        DocumentForm IContainer.Form => this;

        JsonPath IContainer.Path => JsonPath.Empty;

        JsonPath IContainer.GetChildPath(Element child) => JsonPath.Empty;

        bool IContainer.IsChildActive => true;

        private void ExecuteTransaction(ITransaction transaction, Action<ITransaction> fun)
        {
            currentTransaction = transaction;
            OnTransactionStarted?.Invoke(transaction);
            try
            {
                fun(transaction);
            }
            finally
            {
                foreach (var service in services.Values)
                    service.ProcessTransaction(transaction, TransactionStage.ContentUpdateFinalization);
                foreach (var service in services.Values)
                    service.ProcessTransaction(transaction, TransactionStage.ExternalValidation);
                Invalidate(transaction);
                foreach (var service in services.Values)
                    service.ProcessTransaction(transaction, TransactionStage.Cleanup);
                transaction.PostUpdate();
                if (transaction.ShouldRefreshPresentation)
                    RefreshPresentation();
                if (transaction.ExpandIntoView != null)
                    ExpandIntoView(transaction.ExpandIntoView);
                if (transaction is Transaction { HasUndoRedo: true } t)
                    PushUndoRedo(t);
                OnTransactionFinished?.Invoke(transaction);
                currentTransaction = null;
            }
        }

        public void RefreshPresentation()
        {
            OnRefreshPresentation?.Invoke();
        }

        public void ExpandIntoView(Element element)
        {
            OnExpandIntoView?.Invoke(element);
        }

        private void PushUndoRedo(Transaction transaction)
        {
            var head = History.Head;
            if (transaction.Undo == null && transaction.Redo == null && transaction.GroupKey != null && head != null && ReferenceEquals(head.GroupKey, transaction.GroupKey) && (DateTime.Now - lastAction).TotalMilliseconds < 1500)
            {
                head.Append(transaction.GroupUndo, transaction.GroupRedo);
            }
            else
            {
                head?.Seal();
                History.Push(new UndoStep(transaction.Undo, transaction.Redo, transaction.GroupKey, transaction.GroupUndo, transaction.GroupRedo, transaction.ShouldRefreshPresentation));
            }
            lastAction = DateTime.Now;
        }

        void IUndoHandler<UndoStep>.Undo(UndoStep step, ITransaction transaction)
        {
            step.Seal();
            if (step.RefreshPresentation)
                transaction.RefreshPresentation();
            ExecuteTransaction(transaction, step.DoUndo);
        }

        void IUndoHandler<UndoStep>.Redo(UndoStep step, ITransaction transaction)
        {
            if (step.RefreshPresentation)
                transaction.RefreshPresentation();
            ExecuteTransaction(transaction, step.DoRedo);
        }

        public void SealUndo()
        {
            var head = History.Head;
            head?.Seal();
        }

        private void Invalidate(ITransaction transaction)
        {
            Root.Invalidate(transaction.IsFullInvalidationRequested, transaction);
            isModified.Value = Root.IsModified;
        }

        public void SetOriginalJson(ImmutableJson? originalJson)
        {
            Run(transaction =>
            {
                transaction.RequestFullInvalidation();
                Root.SetOriginalJson(originalJson, transaction);
                Invalidate(transaction);
            });
        }

        public T GetService<T>() where T : class, IFormService, new() => (T)services.GetOrAdd(typeof(T), () => new T());

        public T GetService<T>(Func<T> creator) where T : class, IFormService => (T)services.GetOrAdd(typeof(T), creator);

        public void AddService<T>(T service) where T : IFormService => services.Add(typeof(T), service);

        public void RemoveService<T>() => services.Remove(typeof(T));
    }
}