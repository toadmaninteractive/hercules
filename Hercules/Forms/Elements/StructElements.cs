using Hercules.Forms.Presentation;
using Hercules.Forms.Schema;
using Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace Hercules.Forms.Elements
{
    public class Field : ProxyElement, ISortableElement
    {
        public string Name => SchemaField.Name;
        public SchemaField SchemaField { get; }
        public override SchemaType Type => SchemaField.Type;

        public BaseStructElement Struct => (BaseStructElement)Parent;

        public bool IsVisibleInPropertyEditor => SchemaField.IsVisibleInPropertyEditor;

        public Field(BaseStructElement parent, SchemaField schemaField, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, transaction)
        {
            this.SchemaField = schemaField;
            this.Element = Form.Factory.Create(this, schemaField.Type, json, originalJson, transaction);
        }

        public string Caption => SchemaField.Caption;

        public override JsonPath Path => Parent.Path.AppendObjectKey(Name);
        public override JsonPath ValuePath => Parent.Path.AppendObject(Name);

        public override void Present(PresentationContext context)
        {
            var visible = true;
            if (!Form.IsOptionFieldsVisible && Element is OptionalElement optionalElement)
            {
                visible = optionalElement.IsSet;
            }
            if (visible)
            {
                var proxy = context.GetProxy(this);
                if (!context.Compact)
                    context.AddRow(proxy);

                if (context.IsPropertyEditor)
                {
                    context.AddItem(proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool("FieldElementProperty"), isSelectable: true, gridColumn: 1));
                    Element.Present(context);
                }
                else
                {
                    var width = context.Compact ? SchemaField.TextWidth + 8 : Struct.FieldCaptionWidth;
                    proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool("FieldElement"), isSelectable: true, width: width);
                    context.AddItem(proxy.Item0);
                    if (Element is not OptionalElement && !context.Compact)
                        context.AddItem(proxy.Item1 ??= new VirtualRowItem(this, ControlPools.GetPool("RequiredElement"), width: 16));
                    Element.Present(context);
                }
            }
        }

        public void Sort()
        {
            if (Element is ISortableElement sortable)
            {
                sortable.Sort();
            }
        }

        public bool CanSort => Element is ISortableElement { CanSort: true };
    }

    public abstract class BaseStructElement : ExpanderElement<Field>
    {
        protected BaseStructElement(IContainer parent, SchemaType type, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, originalJson, transaction)
        {
            this.IsActualized = false;
            this.Type = type;
        }

        public override SchemaType Type { get; }

        public double CaptionWidth { get; protected set; }
        public double FieldCaptionWidth => Math.Max(CaptionWidth + 8, 45);

        public Field? CaptionField { get; private set; }

        public Field? EnabledField { get; private set; }

        protected virtual SchemaField? CaptionSchemaField => null;

        protected virtual SchemaField? EnabledSchemaField => null;

        protected abstract string DefaultCaption { get; }

        public override Element GetByPath(JsonPath path)
        {
            var head = path.Head;
            Element result = this;
            if (head is JsonObjectKeyPathNode objectKey)
            {
                var field = this.Children.FirstOrDefault(p => p.Name == objectKey.Key);
                if (field != null)
                    result = field;
            }
            else if (head is JsonObjectPathNode key)
            {
                var field = this.Children.FirstOrDefault(p => p.Name == key.Key);
                if (field != null)
                    result = field.Element.GetByPath(path.Tail);
            }
            return result;
        }

        protected void AddChildren(IEnumerable<SchemaField> fields, ImmutableJson? json, ITransaction transaction)
        {
            foreach (var item in fields)
                AddChild(item, json, transaction);
        }

        protected Field AddChild(SchemaField schemaField, ImmutableJson? json, ITransaction transaction)
        {
            if (json == null || !json.IsObject || !json.AsObject.TryGetValue(schemaField.Name, out var value))
                value = null;
            ImmutableJson? originalValue = ReferenceEquals(json, OriginalJson) ? value : SafeJson.GetField(OriginalJson, schemaField.Name);
            var entry = new Field(this, schemaField, value, originalValue, transaction);
            Children.Add(entry);
            if (CaptionSchemaField == schemaField)
            {
                CaptionField = entry;
                CaptionField.DeepElement.PropertyChanged += CaptionField_PropertyChanged;
            }
            if (EnabledSchemaField == schemaField)
                EnabledField = entry;
            return entry;
        }

        private void CaptionField_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdateCaption();
        }

        protected void UpdateCaption()
        {
            if (CaptionField != null)
            {
                var caption = CaptionField.JsonKey;
                Caption = string.IsNullOrEmpty(caption) ? DefaultCaption : caption;
            }
            else
            {
                Caption = DefaultCaption;
            }
        }

        protected JsonObject JsonObject
        {
            get
            {
                var res = new JsonObject();
                foreach (var el in Children)
                    res[el.Name] = el.Element.Json;
                return res;
            }
        }

        public override ImmutableJson Json => JsonObject;

        public override void SetOriginalJson(ImmutableJson? newOriginalJson, ITransaction transaction)
        {
            base.SetOriginalJson(newOriginalJson, transaction);
            foreach (var field in Children)
            {
                field.SetOriginalJson(SafeJson.GetField(newOriginalJson, field.Name), transaction);
            }
        }

        protected override bool IsOverriden(ImmutableJson? baseJson)
        {
            var result = false;
            foreach (var field in Children)
            {
                if (field.Inherit(SafeJson.GetField(baseJson, field.Name)))
                    result = true;
            }
            return result;
        }

        protected override bool IsContainerModified() => OriginalJson == null || !OriginalJson.IsObject;

        public override void Present(PresentationContext context)
        {
            var proxy = context.GetProxy(this);

            if (context.IsPropertyEditor)
            {
                context.AddItem(proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool("ExpanderElementPropertyToggle"), gridColumn: 0, width: 16));
                context.AddItem(proxy.Item1 ??= new VirtualRowItem(this, ControlPools.GetPool("ExpanderElementPropertyContent")));

                if (IsExpanded)
                {
                    context.Indent(16);
                    foreach (var child in GetChildren())
                    {
                        child.Present(context);
                    }
                    context.Outdent();
                }
            }
            else
            {
                var pool = EnabledSchemaField == null ? "ExpanderElement" : "CheckedExpanderElement";
                if (IsExpanded)
                {
                    var offset = FieldCaptionWidth;
                    var maxOffset = Form.Settings.MaxStructFieldLabelSize.Value;
                    if (offset > maxOffset)
                        context.Indent(context.Left + maxOffset - offset);
                    else
                        context.Indent(context.Left);

                    context.AddItem(proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool(pool), isTabStop: true));
                    foreach (var child in GetChildren())
                    {
                        if (child != EnabledField)
                            child.Present(context);
                    }

                    context.Outdent();
                }
                else
                {
                    context.AddItem(proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool(pool), isTabStop: true), height: 22);
                }
            }
        }
    }

    public class RecordElement : BaseStructElement
    {
        public RecordElement(IContainer parent, RecordSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, type, originalJson, transaction)
        {
            this.RecordType = type;
            if (json != null && json.IsObject || IsActive || RecordType.Compact)
                Actualize(json, transaction);

            if (type.AllFields.Any())
                CaptionWidth = type.AllFields.Max(p => p.TextWidth);
            else
                CaptionWidth = 10f;
            UpdateCaption();
        }

        public RecordSchemaType RecordType { get; }

        protected override SchemaField? CaptionSchemaField => RecordType.Record.CaptionField;

        protected override SchemaField? EnabledSchemaField => RecordType.Record.EnabledField;

        protected override string DefaultCaption => $"<{RecordType.Record.Name}>";

        protected override void DoActualize(ImmutableJson? json, ITransaction transaction)
        {
            AddChildren(RecordType.AllFields, json, transaction);
        }

        public override void SetJson(ImmutableJson? json, ITransaction transaction)
        {
            if (json != null && json.IsObject)
            {
                if (!IsActualized)
                {
                    Actualize(null, transaction);
                    ValueChanged(transaction);
                }

                foreach (var child in Children)
                {
                    child.SetJson(json.GetValueSafe(child.Name), transaction);
                }
            }
            else
                transaction.AddWarning(Path, "Invalid value");
        }

        public override void Present(PresentationContext context)
        {
            PresentColorEditor(context);
            if (RecordType.Compact && !context.IsPropertyEditor)
            {
                context.Compact = true;
                bool needMargin = false;
                foreach (var child in GetChildren())
                {
                    if (needMargin)
                        context.AddMargin(4);
                    child.Present(context);
                    needMargin = true;
                }
                context.Compact = false;
            }
            else
            {
                base.Present(context);
            }
        }

        private void PresentColorEditor(PresentationContext context)
        {
            if (RecordType.Record.ColorSchema != null)
            {
                var proxy = context.GetProxy(this);
                if (proxy.Item2 == null)
                {
                    proxy.Item2 = new VirtualRowItem(this, ControlPools.GetPool("ColorElement"), width: 33, popupPool: ControlPools.GetPool("ColorElementPopup"), isSelectable: true);
                    var colorBehavior = new ColorEditorBehavior(proxy.Item2, this, RecordType.Record.ColorSchema);
                    proxy.Item2.AddBehavior(colorBehavior);
                }
                context.AddItem(proxy.Item2);
                context.AddMargin(4);
            }
        }
    }

    public class VariantElement : BaseStructElement
    {
        public Field? TagField { get; private set; }

        readonly Dictionary<string, List<Field>> cache = new Dictionary<string, List<Field>>();

        public string? TagValue { get; private set; }

        public VariantElement(IContainer parent, VariantSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, type, originalJson, transaction)
        {
            this.VariantType = type;
            if ((json != null && json.IsObject) || IsActive)
            {
                Actualize(json, transaction);
            }
            CaptionWidth = Variant.AllChildrenFields.Max(f => f.TextWidth);
            UpdateCaption();
        }

        public VariantSchemaType VariantType { get; }

        public SchemaVariant Variant => VariantType.Variant;

        protected override SchemaField? CaptionSchemaField => Variant.CaptionField;

        protected override SchemaField? EnabledSchemaField => Variant.EnabledField;

        public SchemaRecord? VariantChild => TagValue == null ? null : Variant.GetChild(TagValue);

        protected override void DoActualize(ImmutableJson? json, ITransaction transaction)
        {
            foreach (var f in Variant.Fields)
            {
                var field = AddChild(f, json, transaction);
                if (f.IsTag)
                {
                    TagField = field;
                    TagField.Events.OnValueChanged += TagField_OnValueChanged;
                }
            }
            UpdateDescendants(json, transaction);
        }

        private void TagField_OnValueChanged(Element element, ITransaction transaction)
        {
            if (TagValue != TagField!.JsonKey)
            {
                UpdateDescendants(null, transaction);

                void Update(ITransaction t)
                {
                    UpdateDescendants(null, t);
                    ValueChanged(t);
                }

                transaction.AddUndoRedo(Update, Update); // TODO: why is that necessary?
                transaction.RefreshPresentation();
                transaction.RequestFullInvalidation();
            }
        }

        void UpdateDescendants(ImmutableJson? json, ITransaction transaction)
        {
            if (TagValue != null)
            {
                foreach (var f in cache[TagValue])
                    Children.Remove(f);
            }
            TagValue = TagField!.JsonKey;
            if (TagValue != null)
            {
                if (cache.ContainsKey(TagValue))
                {
                    foreach (var f in cache[TagValue])
                        Children.Add(f);
                }
                else
                {
                    var fields = new List<Field>();
                    var child = VariantChild;
                    if (child != null)
                    {
                        foreach (var f in child.FieldsUpTo(Variant))
                            fields.Add(AddChild(f, json, transaction));
                    }
                    cache[TagValue] = fields;
                }
            }
            TransactionalPropertyChanged(nameof(TagValue), transaction);
            UpdateCaption();
        }

        protected override string DefaultCaption
        {
            get
            {
                string recordName = Variant.Name;
                if (TagValue != null)
                {
                    var child = Variant.GetChild(TagValue);
                    if (child != null)
                        recordName = child.Name;
                }
                return $"<{recordName}>";
            }
        }

        public override void SetJson(ImmutableJson? json, ITransaction transaction)
        {
            if (json != null && json.IsObject)
            {
                if (!IsActualized)
                {
                    Actualize(null, transaction);
                    ValueChanged(transaction);
                }

                var newTagJson = json.GetValueSafe(TagField.Name);
                if (!ImmutableJson.Equals(TagField.Json, newTagJson))
                {
                    TagField!.SetJson(newTagJson, transaction);
                    UpdateDescendants(json, transaction);
                }

                foreach (var child in Children)
                {
                    if (child != TagField)
                        child.SetJson(json.GetValueSafe(child.Name), transaction);
                }
            }
            else
                transaction.AddWarning(Path, "Invalid value");
        }

        public override void SetOriginalJson(ImmutableJson? newOriginalJson, ITransaction transaction)
        {
            base.SetOriginalJson(newOriginalJson, transaction);
            foreach (var key in cache.Keys.ToList())
                if (key != TagValue)
                    cache.Remove(key);
        }
    }
}
