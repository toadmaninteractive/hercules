using Hercules.Controls;
using Hercules.Forms.Presentation;
using Hercules.Forms.Schema;
using Json;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Hercules.Forms.Elements
{
    /// <summary>
    /// It is slots name-selector control. For diagrams links.
    /// </summary>
    public class SlotElement : NullableReferenceElement<string, StringSchemaType>, IAutoCompleteElement
    {
        public ObservableCollection<string> Source { get; } = new ObservableCollection<string>();

        public SlotElement(IContainer parent, StringSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, type, json, originalJson, transaction)
        { }

        protected override void Validate(bool invalidateChildren, ITransaction transaction, out bool isValid, out bool isModified)
        {
            base.Validate(invalidateChildren, transaction, out isValid, out isModified);
            isValid = isValid && IsValidSlot();
        }

        private bool IsValidSlot()
        {
            return !string.IsNullOrEmpty(Value) && Source.Contains(Value);
        }

        public void Bind(RefElement refElement)
        {
            refElement.Events.OnPropertyChanged += OnRefValueChanged;
        }

        private void OnRefValueChanged(Element element, string propertyName, ITransaction transaction)
        {
            if (propertyName == nameof(RefElement.ValueElement))
            {
                var blockElement = ((RefElement)element).ValueElement.GetParentByType<BlockListItem>();
                if (blockElement?.Prototype != null)
                {
                    Source.SynchronizeSorted(blockElement.Prototype.Connectors.Where(x => x.Kind == ConnectorKind.In || x.Kind == ConnectorKind.Out).Select(x => x.Name).Order());
                }
                else
                {
                    Source.Clear();
                }
                ValueChanged(transaction);
            }
        }

        public override void Present(PresentationContext context)
        {
            var proxy = context.GetProxy(this);
            if (proxy.Item0 == null)
            {
                var dock = context.FlexibleDock;
                proxy.Item0 = new VirtualRowItem(this, ControlPools.GetPool("EnumElementPreview"), editorPool: ControlPools.GetPool("EnumElementEditor"), popupPool: ControlPools.GetPool("EnumElementPopup"), dock: dock, width: 320);
                proxy.Item0.AddBehavior(new AutoCompleteBehavior(proxy.Item0, this));
            }

            context.AddItem(proxy.Item0);
        }

        object IAutoCompleteElement.Items => Source;

        public void Submit(object? suggestion)
        {
            if (suggestion == null)
            {
                if (Source.FirstOrDefault(Filter) is string value)
                {
                    Value = value;
                }
            }
            else
            {
                Value = (string)suggestion;
            }
        }

        public bool Filter(object? item)
        {
            if (item != null)
            {
                string filter = Value ?? string.Empty;
                string val = (string)item;
                if (val.SmartFilter(filter))
                    return true;
                return false;
            }
            return false;
        }
    }

    public class BlockListItem : ListItem
    {
        private readonly JsonPath PathRef = new JsonPath("ref");

        public VariantElement VariantElement => (VariantElement)Element;

        public BaseStructElement StructElement => (BaseStructElement)Element;

        public StringElement RefElement { get; }

        public SchemaBlock? Prototype { get; private set; }

        public BlockListItem(ListElement parent, SchemaType type, ImmutableJson? json, ImmutableJson? originalJson, int index, int? originalIndex, ITransaction transaction)
            : base(parent, type, json, originalJson, index, originalIndex, transaction)
        {
            RefElement = (StringElement)GetByPath(PathRef);
            if (RefElement == null)
                throw new InvalidOperationException("ref field is not found");

            if (Element is VariantElement variantElement)
            {
                Prototype = variantElement.VariantChild?.Block;
                VariantElement.Events.OnPropertyChanged += VariantElement_OnPropertyChanged;
            }
            else if (Element is RecordElement recordElement)
                Prototype = recordElement.RecordType.Record.Block;
            else
                throw new InvalidOperationException("BlockListItem child should be Variant or Record");
        }

        private void VariantElement_OnPropertyChanged(Element element, string propertyName, ITransaction transaction)
        {
            if (propertyName == nameof(VariantElement.TagValue))
            {
                var newPrototype = VariantElement.VariantChild?.Block;
                if (newPrototype != Prototype)
                {
                    Prototype = newPrototype;
                    transaction.RequestFullInvalidation();
                }
            }
        }
    }

    public class LinkListItem : ListItem
    {
        private static readonly JsonPath PathToBlock = new JsonPath("to", "block");
        private static readonly JsonPath PathToSlot = new JsonPath("to", "slot");
        private static readonly JsonPath PathFromBlock = new JsonPath("from", "block");
        private static readonly JsonPath PathFromSlot = new JsonPath("from", "slot");

        public RefElement ToRefElement { get; }
        public RefElement FromRefElement { get; }
        public SlotElement ToSlotElement { get; }
        public SlotElement FromSlotElement { get; }

        public LinkListItem(ListElement parent, SchemaType type, ImmutableJson? json, ImmutableJson? originalJson, int index, int? originalIndex, ITransaction transaction)
            : base(parent, type, json, originalJson, index, originalIndex, transaction)
        {
            ToRefElement = (RefElement)GetByPath(PathToBlock);
            FromRefElement = (RefElement)GetByPath(PathFromBlock);
            ToSlotElement = (SlotElement)GetByPath(PathToSlot);
            FromSlotElement = (SlotElement)GetByPath(PathFromSlot);
            ToSlotElement.Bind(ToRefElement);
            FromSlotElement.Bind(FromRefElement);
        }

        protected override void Validate(bool invalidateChildren, ITransaction transaction, out bool isValid, out bool isModified)
        {
            base.Validate(invalidateChildren, transaction, out isValid, out isModified);
            isValid = isValid && IsValidLink();
        }

        private bool IsValidLink()
        {
            var isValid = ToRefElement.Value != FromRefElement.Value;
            /*
             * check for uniquness - TODO: optimize
            if (isValid)
            {
                foreach (LinkListItem element in parent.Children)
                {
                    if (element == this)
                        continue;

                    var elementDataObject = element.GetDataObject();
                    isValid = !elementDataObject.Equals(currentDataObject);
                }
            }*/

            return isValid;
        }
    }
}
