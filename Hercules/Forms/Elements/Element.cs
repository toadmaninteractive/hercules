using Hercules.Forms.Presentation;
using Hercules.Forms.Schema;
using Json;
using System;
using System.Runtime.CompilerServices;

namespace Hercules.Forms.Elements
{
    [Flags]
    public enum VisitOptions
    {
        None = 0,
        ChildrenFirst,
    }

    [Flags]
    public enum ElementFlags
    {
        None = 0,

        /// <summary>
        /// Element and all its children are valid
        /// </summary>
        Valid = 1,

        /// <summary>
        /// Element and all its children are the same as original document values
        /// </summary>
        Modified = 2,

        /// <summary>
        /// Element is active (unset optional elements and removed collection items are not active)
        /// </summary>
        Active = 4,

        /// <summary>
        /// Element content has been created (can be false for lazy loaded inactive elements)
        /// </summary>
        Actualized = 16,

        /// <summary>
        /// Optional value is set
        /// </summary>
        Set = 32,

        /// <summary>
        /// Expandable element is expanded
        /// </summary>
        Expanded = 64,

        /// <summary>
        /// Element is overriden in the inherited document
        /// </summary>
        Inherited = 128,

        /// <summary>
        /// Element is marked as invalid by external validator service
        /// </summary>
        ExternalInvalid = 256,

        /// <summary>
        /// If element was modified during transaction, PostUpdate should be called
        /// </summary>
        HasPostUpdate = 512,

        /// <summary>
        /// If element preview is available
        /// </summary>
        HasPreview = 1024,
    }

    /// <summary>
    /// Incapsulates all element events to a separate class which is not instantiated when there're no subscribers to save memory
    /// </summary>
    public class ElementEvents
    {
        private readonly Element element;

        public delegate void ElementActiveChangedEvent(Element element, bool isActive, ITransaction transaction);

        public delegate void ElementValueChangedEvent(Element element, ITransaction transaction);

        public delegate void ElementPropertyChangedEvent(Element element, string propertyName, ITransaction transaction);

        /// <summary>
        /// Triggered when element's active state has changed.
        /// </summary>
        public event ElementActiveChangedEvent? OnActiveChanged;

        /// <summary>
        /// Triggered when element's content has changed
        /// </summary>
        public event ElementValueChangedEvent? OnValueChanged;

        /// <summary>
        /// Triggered when element's property changed. This is implementation specific. Different elements can support different properties.
        /// </summary>
        public event ElementPropertyChangedEvent? OnPropertyChanged;

        public void ActiveChanged(bool isActive, ITransaction transaction) => OnActiveChanged?.Invoke(element, isActive, transaction);

        public void ValueChanged(ITransaction transaction) => OnValueChanged?.Invoke(element, transaction);

        public void PropertyChanged(string propertyName, ITransaction transaction) => OnPropertyChanged?.Invoke(element, propertyName, transaction);

        public ElementEvents(Element element)
        {
            this.element = element;
        }
    }

    public abstract class Element : NotifyPropertyChanged, IComparable<Element>
    {
        protected Element(IContainer parent, ITransaction transaction)
        {
            this.lastTransactionId = transaction.TransactionId;
            this.Parent = parent;
            this.Form = parent.Form;
            this.elementFlags = ElementFlags.Valid | ElementFlags.Actualized;
            if (parent.IsChildActive)
                elementFlags |= ElementFlags.Active;
        }

        public DocumentForm Form { get; }

        private ElementFlags elementFlags;

        public bool IsValid
        {
            get => elementFlags.HasFlag(ElementFlags.Valid);
            private set => SetFlag(ElementFlags.Valid, value);
        }

        public bool IsModified
        {
            get => elementFlags.HasFlag(ElementFlags.Modified);
            private set => SetFlag(ElementFlags.Modified, value);
        }

        public bool IsInherited
        {
            get => GetFlag(ElementFlags.Inherited);
            set => SetFlag(ElementFlags.Inherited, value);
        }

        public bool IsActive => elementFlags.HasFlag(ElementFlags.Active);

        protected bool IsActualized
        {
            get => elementFlags.HasFlag(ElementFlags.Actualized);
            set => SetFlag(ElementFlags.Actualized, value, null);
        }

        protected bool HasPostUpdate
        {
            get => elementFlags.HasFlag(ElementFlags.HasPostUpdate);
            set => SetFlag(ElementFlags.HasPostUpdate, value, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool GetFlag(ElementFlags flag) => elementFlags.HasFlag(flag);

        protected bool SetFlag(ElementFlags flag, bool value, [CallerMemberName] string? propertyName = null)
        {
            if (elementFlags.HasFlag(flag) != value)
            {
                if (value)
                    elementFlags |= flag;
                else
                    elementFlags &= ~flag;
                if (propertyName != null)
                    RaisePropertyChanged(propertyName);
                return true;
            }

            return false;
        }

        private ElementEvents? events;

        public ElementEvents Events => events ??= new ElementEvents(this);

        public abstract SchemaType Type { get; }

        public abstract ImmutableJson? OriginalJson { get; }

        public bool IsJsonKey => Parent.IsJsonKeyChild(this);

        protected void SetIsActive(bool isActive, ITransaction transaction)
        {
            if (SetFlag(ElementFlags.Active, isActive, nameof(IsActive)))
            {
                ActiveChanged(isActive, transaction);
                ValueChanged(transaction);
            }
        }

        protected void TransactionalPropertyChanged(string propertyName, ITransaction transaction)
        {
            events?.PropertyChanged(propertyName, transaction);
        }

        /// <summary>
        /// Marks this element as modified by the transaction.
        /// </summary>
        /// <param name="transaction">Transaction</param>
        protected void ValueChanged(ITransaction transaction)
        {
            if (lastTransactionId == transaction.TransactionId)
                return;
            lastTransactionId = transaction.TransactionId;
            OnValueUpdate(transaction);
            if (HasPostUpdate)
                transaction.OnPostUpdate += PostUpdate;
            events?.ValueChanged(transaction);
            Parent.ChildChanged(transaction);
        }

        protected virtual void OnValueUpdate(ITransaction transaction)
        {
        }

        protected virtual void PostUpdate()
        {
        }

        public void Invalidate(bool invalidateChildren, ITransaction transaction)
        {
            if (invalidateChildren || WasModified(transaction))
            {
                Validate(invalidateChildren, transaction, out var newIsValid, out var newIsModified);
                IsValid = newIsValid && !elementFlags.HasFlag(ElementFlags.ExternalInvalid);
                IsModified = newIsModified;
            }
        }

        public void SetExternalValidationResult(bool isValid, ITransaction transaction)
        {
            if (SetFlag(ElementFlags.ExternalInvalid, !isValid, null))
                ValueChanged(transaction);
        }

        public IContainer Parent { get; }

        protected abstract void Validate(bool invalidateChildren, ITransaction transaction, out bool isValid, out bool isModified);

        protected virtual void DoActualize(ImmutableJson? json, ITransaction transaction)
        {
        }

        protected void Actualize(ImmutableJson? json, ITransaction transaction)
        {
            if (!IsActualized)
            {
                DoActualize(json, transaction);
                IsActualized = true;
            }
        }

        private int lastTransactionId;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool WasModified(ITransaction transaction) => lastTransactionId >= transaction.TransactionId;

        public virtual void Visit(Action<Element> visitor, VisitOptions options)
        {
            visitor(this);
        }

        public abstract ImmutableJson Json { get; }
        public abstract string JsonKey { get; }

        public abstract void SetOriginalJson(ImmutableJson? newOriginalJson, ITransaction transaction);

        public bool Inherit(ImmutableJson? baseJson)
        {
            var result = IsOverriden(baseJson);
            IsInherited = !result;
            if (result && !(this is ProxyElement) && Type.IsAtomic)
                Visit(element => element.IsInherited = false, VisitOptions.None);
            return result;
        }

        protected abstract bool IsOverriden(ImmutableJson? baseJson);

        public abstract void SetJson(ImmutableJson? json, ITransaction transaction);

        public virtual void SetJsonKey(string jsonKey, ITransaction transaction)
        {
            throw new NotSupportedException();
        }

        public virtual Element GetByPath(JsonPath path) => this;

        public virtual JsonPath Path => Parent.GetChildPath(this);
        public virtual JsonPath ValuePath => Path;

        public void ParentActiveChanged(bool isActive, ITransaction transaction)
        {
            SetIsActive(isActive, transaction);
        }

        protected virtual void ActiveChanged(bool isActive, ITransaction transaction)
        {
            events?.ActiveChanged(isActive, transaction);
        }

        public abstract void Present(PresentationContext context);

        public virtual int CompareTo(Element? other) => 0;
    }
}