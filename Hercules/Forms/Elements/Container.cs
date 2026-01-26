using Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Hercules.Forms.Elements
{
    public interface IContainer
    {
        bool IsChildActive { get; }

        void ChildChanged(ITransaction transaction);

        DocumentForm Form { get; }
        JsonPath Path { get; }

        JsonPath GetChildPath(Element child);
        bool IsJsonKeyChild(Element child);
    }

    public abstract class Container : Element, IContainer
    {
        public virtual bool IsChildActive => IsActive;

        protected Container(IContainer parent, ITransaction transaction)
            : base(parent, transaction)
        {
        }

        public abstract IEnumerable<Element> GetChildren();

        protected override void Validate(bool invalidateChildren, ITransaction transaction, out bool isValid, out bool isModified)
        {
            isValid = true;
            isModified = IsContainerModified();
            foreach (var child in GetChildren())
            {
                child.Invalidate(invalidateChildren, transaction);
                isValid = isValid && child.IsValid;
                isModified = isModified || child.IsModified;
            }
        }

        protected virtual bool IsContainerModified() => false;

        public override void Visit(Action<Element> visitor, VisitOptions options)
        {
            if (!options.HasFlag(VisitOptions.ChildrenFirst))
                base.Visit(visitor, options);
            foreach (var child in GetChildren())
                child.Visit(visitor, options);
            if (options.HasFlag(VisitOptions.ChildrenFirst))
                base.Visit(visitor, options);
        }

        public override string JsonKey => throw new NotSupportedException("Containers cannot be JSON keys");

        public virtual JsonPath GetChildPath(Element child) => ValuePath;

        protected override void ActiveChanged(bool isActive, ITransaction transaction)
        {
            var childActive = IsChildActive;
            foreach (var child in GetChildren())
            {
                child.ParentActiveChanged(childActive, transaction);
            }
            base.ActiveChanged(isActive, transaction);
        }

        public virtual void ChildChanged(ITransaction transaction)
        {
            ValueChanged(transaction);
        }

        public virtual bool IsJsonKeyChild(Element child) => false;
    }

    public abstract class ExpanderElement<T> : Container, IExpandableElement
        where T : Element
    {
        protected ExpanderElement(IContainer parent, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, transaction)
        {
            this.originalJson = originalJson;
            bool isExpanded = false;
            if (IsActive)
            {
                if (Parent is RootElement)
                    isExpanded = true;
                else if (Form.Settings.ExpandNewDocument.Value == ExpandElementType.ExpandTree)
                    isExpanded = true;
                else if (Form.Settings.ExpandNewDocument.Value == ExpandElementType.Expand)
                {
                    if (parent is Element parentElement)
                    {
                        var expandableParent = parentElement.GetParentByType<IExpandableElement>();
                        isExpanded = expandableParent is RecordElement { Parent: RootElement };
                    }
                }
            }

            SetFlag(ElementFlags.Expanded, isExpanded, null);
        }

        public override ImmutableJson? OriginalJson => originalJson;
        private ImmutableJson? originalJson;

        public ObservableCollection<T> Children { get; } = new ObservableCollection<T>();

        public bool IsExpanded
        {
            get => GetFlag(ElementFlags.Expanded);
            set
            {
                if (IsExpanded != value)
                    Form.Run(transaction => Expand(value, transaction));
            }
        }

        public void Expand(bool expanded, ITransaction transaction)
        {
            if (SetFlag(ElementFlags.Expanded, expanded, nameof(IsExpanded)))
            {
                if (expanded && !IsActualized)
                {
                    Actualize(null, transaction);
                    ValueChanged(transaction);
                }

                transaction.RefreshPresentation();
            }
        }

        public override void SetOriginalJson(ImmutableJson? newOriginalJson, ITransaction transaction)
        {
            originalJson = newOriginalJson;
        }

        string? caption;

        public string? Caption
        {
            get => caption;
            protected set => SetField(ref caption, value);
        }

        public override IEnumerable<Element> GetChildren() => Children;
    }
}