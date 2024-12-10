using Hercules.Controls;
using Hercules.Forms.Presentation;
using Hercules.Forms.Schema;
using Json;
using System;
using System.Windows.Input;

namespace Hercules.Forms.Elements
{
    public abstract class ProxyElement : Element, IContainer
    {
        /// <summary>
        /// The single child element that is wrapped by this proxy. It should be always set once in the descendant constructors.
        /// </summary>
        public Element Element { get; protected set; } = default!;

        public override SchemaType Type => Element.Type;

        public Element DeepElement
        {
            get
            {
                var element = Element;
                while (element is ProxyElement proxyElement)
                    element = proxyElement.Element;
                return element;
            }
        }

        public virtual bool IsChildActive => IsActive;

        public virtual void ChildChanged(ITransaction transaction)
        {
            ValueChanged(transaction);
        }

        protected ProxyElement(IContainer parent, ITransaction transaction)
            : base(parent, transaction)
        {
        }

        protected override void Validate(bool invalidateChildren, ITransaction transaction, out bool isValid, out bool isModified)
        {
            Element.Invalidate(true, transaction);
            isValid = Element.IsValid;
            isModified = Element.IsModified;
        }

        public override void Visit(Action<Element> visitor, VisitOptions options)
        {
            if (options.HasFlag(VisitOptions.ChildrenFirst))
            {
                Element.Visit(visitor, options);
                visitor(this);
            }
            else
            {
                visitor(this);
                Element.Visit(visitor, options);
            }
        }

        public override ImmutableJson Json => Element.Json;

        public override string JsonKey => Element.JsonKey;

        public override Element GetByPath(JsonPath path) => Element.GetByPath(path);

        public JsonPath GetChildPath(Element child) => ValuePath;
        public bool IsJsonKeyChild(Element child) => IsJsonKey;

        public override void SetJson(ImmutableJson? json, ITransaction transaction)
        {
            Element.SetJson(json, transaction);
        }

        protected override void ActiveChanged(bool isActive, ITransaction transaction)
        {
            Element.ParentActiveChanged(IsChildActive, transaction);
            base.ActiveChanged(isActive, transaction);
        }

        public override void Present(PresentationContext context)
        {
            Element.Present(context);
        }

        public override void SetOriginalJson(ImmutableJson? newOriginalJson, ITransaction transaction)
        {
            Element.SetOriginalJson(newOriginalJson, transaction);
        }

        protected override bool IsOverriden(ImmutableJson? baseJson)
        {
            return Element.Inherit(baseJson);
        }

        public override ImmutableJson? OriginalJson => Element.OriginalJson;
    }

    public class OptionalElement : ProxyElement
    {
        public OptionalElement(IContainer parent, SchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, transaction)
        {
            this.originalJson = originalJson;
            SetFlag(ElementFlags.Set, json != null && !json.IsNull);
            this.Element = Form.Factory.CreateNotOptional(this, type, json, originalJson, transaction);
            this.ToggleCommand = Commands.Execute(DoToggle);
        }

        public override ImmutableJson? OriginalJson => originalJson;
        private ImmutableJson? originalJson;

        public override bool IsChildActive => IsActive && IsSet;

        public bool IsSet => GetFlag(ElementFlags.Set);

        private void IsSetChanged(bool isSet, ITransaction transaction)
        {
            ActiveChanged(isSet, transaction);
            ValueChanged(transaction);
        }

        public void SetIsSet(bool value, ITransaction transaction)
        {
            if (SetFlag(ElementFlags.Set, value, nameof(IsSet)))
            {
                void Undo(ITransaction t)
                {
                    SetFlag(ElementFlags.Set, !value, nameof(IsSet));
                    IsSetChanged(!value, t);
                }

                void Redo(ITransaction t)
                {
                    SetFlag(ElementFlags.Set, value, nameof(IsSet));
                    IsSetChanged(value, t);
                }

                transaction.AddUndoRedo(Undo, Redo);
                IsSetChanged(value, transaction);
            }
        }

        public ICommand ToggleCommand { get; }

        void DoToggle()
        {
            Form.Run(transaction => SetIsSet(!IsSet, transaction), this);
        }

        protected override void Validate(bool invalidateChildren, ITransaction transaction, out bool isValid, out bool isModified)
        {
            if (IsSet)
            {
                base.Validate(invalidateChildren, transaction, out isValid, out isModified);
                isModified = isModified || OriginalJson == null || OriginalJson.IsNull;
            }
            else
            {
                isValid = true;
                isModified = !(OriginalJson == null || OriginalJson.IsNull);
            }
        }

        protected override bool IsOverriden(ImmutableJson? baseJson)
        {
            if (IsSet)
            {
                return base.IsOverriden(baseJson) || baseJson == null || baseJson.IsNull;
            }
            else
            {
                return !(baseJson == null || baseJson.IsNull);
            }
        }

        public override void Visit(Action<Element> visitor, VisitOptions options)
        {
            if (!options.HasFlag(VisitOptions.ChildrenFirst))
                visitor(this);
            if (IsSet)
                Element.Visit(visitor, options);
            if (options.HasFlag(VisitOptions.ChildrenFirst))
                visitor(this);
        }

        public override void ChildChanged(ITransaction transaction)
        {
            if (transaction.UserInputElement != null && transaction.UserInputElement != this)
                SetIsSet(true, transaction);
            base.ChildChanged(transaction);
        }

        public override ImmutableJson Json => IsSet ? base.Json : ImmutableJson.Null;

        public override string JsonKey => IsSet ? base.JsonKey : string.Empty;

        public override Element GetByPath(JsonPath path)
        {
            if (IsSet)
                return Element.GetByPath(path);
            else
                return this;
        }

        public override void SetJson(ImmutableJson? json, ITransaction transaction)
        {
            bool newIsSet = json != null && !json.IsNull;
            if (newIsSet != IsSet)
                SetIsSet(newIsSet, transaction);
            if (newIsSet)
                base.SetJson(json, transaction);
        }

        public override void Present(PresentationContext context)
        {
            var proxy = context.GetProxy(this);
            if (context.IsPropertyEditor)
            {
                context.AddItem(proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool("OptionalElementPropertyToggle"), width: 16, isTabStop: true, dock: HorizontalDock.Right));
            }
            else
            {
                context.AddItem(proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool("OptionalElement"), isTabStop: true, width: 16));
            }

            Element.Present(context);
        }

        public override void SetOriginalJson(ImmutableJson? newOriginalJson, ITransaction transaction)
        {
            base.SetOriginalJson(newOriginalJson, transaction);
            originalJson = newOriginalJson;
        }
    }
}