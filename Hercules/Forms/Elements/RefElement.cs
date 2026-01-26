using Hercules.Forms.Presentation;
using Hercules.Forms.Schema;
using Json;
using System.Collections.ObjectModel;
using System.Linq;

namespace Hercules.Forms.Elements
{
    /// <summary>
    /// It is ref-selector control. For diagrams block (and links).
    /// </summary>
    public class RefElement : NullableReferenceElement<string, StringSchemaType>, IAutoCompleteElement
    {
        public ObservableCollection<string> Source { get; }

        private readonly ReferenceController referenceController;

        public StringElement? ValueElement { get; private set; }

        private void SetValueElement(StringElement? newValueElement, ITransaction transaction)
        {
            if (ValueElement != newValueElement)
            {
                ValueElement = newValueElement;
                var newValue = newValueElement == null ? value : newValueElement.Value;
                if (value != newValue)
                    SetValue(newValue, transaction);
                TransactionalPropertyChanged(nameof(ValueElement), transaction);
            }
        }

        protected override void OnValueUpdate(ITransaction transaction)
        {
            if (ValueElement == null || ValueElement.Value != value)
            {
                ValueElement = referenceController.SourceElements.FirstOrDefault(el => el.Value == value);
                TransactionalPropertyChanged(nameof(ValueElement), transaction);
            }
        }

        public RefElement(IContainer parent, StringSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, type, json, originalJson, transaction)
        {
            referenceController = Form.GetService<ElementReferenceService>().RegisterTarget(type.ReferenceTargetId!, this);
            Source = type.Species switch
            {
                StringSchemaType.StringSpecies.RefAsset => new ObservableCollection<string>(),
                StringSchemaType.StringSpecies.RefLink => new ObservableCollection<string>(),
                _ => referenceController.SourceValues
            };
        }

        protected override void Validate(bool invalidateChildren, ITransaction transaction, out bool isValid, out bool isModified)
        {
            base.Validate(invalidateChildren, transaction, out isValid, out isModified);
            if (isValid && SimpleType.RequireValidReference)
            {
                isValid = ValueElement != null && IsValidRef(ValueElement);
            }
        }

        private void UpdateSource()
        {
            switch (this.SimpleType.Species)
            {
                case StringSchemaType.StringSpecies.RefAsset:
                    Source.SynchronizeSorted(
                        from sourceElement in referenceController.SourceElements
                        let block = sourceElement.GetParentByType<BlockListItem>()
                        where block?.Prototype != null
                        where block.Prototype!.Connectors.Any(c => c.Kind == ConnectorKind.Asset)
                        orderby sourceElement.Value
                        select sourceElement.Value);
                    break;
                case StringSchemaType.StringSpecies.RefLink:
                    Source.SynchronizeSorted(
                        from sourceElement in referenceController.SourceElements
                        let block = sourceElement.GetParentByType<BlockListItem>()
                        where block?.Prototype != null
                        where block.Prototype!.Connectors.Any(c => c.Kind == ConnectorKind.In || c.Kind == ConnectorKind.Out)
                        orderby sourceElement.Value
                        select sourceElement.Value);
                    break;
            }
        }

        private bool IsValidRef(StringElement sourceElement)
        {
            switch (this.SimpleType.Species)
            {
                case StringSchemaType.StringSpecies.RefAsset:
                    var parent = this.GetParentByType<Field>();
                    if (parent?.DeepElement is ListElement list)
                    {
                        var selectedIds = list.Children.OfType<ListItem>().Select(x => ((RefElement)x.DeepElement).Value).ToList();
                        if (selectedIds.GroupBy(x => x).Any(g => g.Count() > 1))
                            return false;
                    }
                    break;
            }
            return true;
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

        public void ReferenceSourceChanged(ITransaction transaction)
        {
            if (ValueElement != null && ValueElement.IsActive && ValueElement.WasModified(transaction))
            {
                SetValue(ValueElement.Value, transaction);
            }
            else
            {
                UpdateSource();
                var newValueElement = referenceController.SourceElements.FirstOrDefault(el => el.Value == value);
                SetValueElement(newValueElement, transaction);
            }
        }
    }
}
