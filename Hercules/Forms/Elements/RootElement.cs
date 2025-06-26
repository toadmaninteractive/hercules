using Hercules.Forms.Presentation;
using Hercules.Forms.Schema;
using Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace Hercules.Forms.Elements
{
    public class SchemalessField : ProxyElement
    {
        public string Name { get; }

        public RootElement Root => (RootElement)Parent;

        public SchemalessField(RootElement parent, string name, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, transaction)
        {
            this.Name = name;
            this.Element = new JsonElement(this, new JsonSchemaType(), json, originalJson, transaction);
            this.RemoveCommand = Commands.Execute(Remove);
        }

        public ICommand RemoveCommand { get; }

        void Remove()
        {
            Form.Run(transaction => Root.RemoveSchemalessField(this, transaction), this);
        }

        public string Caption => Name + ":";

        public override JsonPath Path => Parent.Path.AppendObjectKey(Name);
        public override JsonPath ValuePath => Parent.Path.AppendObject(Name);

        public override void Present(PresentationContext context)
        {
            var proxy = context.GetProxy(this);
            context.AddRow(proxy);
            var width = Root.FieldCaptionWidth;
            context.AddItem(proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool("FieldElement"), width: width));
            context.AddItem(proxy.Item1 ??= new VirtualRowItem(this, ControlPools.GetPool("SchemalessFieldRemoveButton"), width: 16));
            Element.Present(context);
        }
    }

    public class RootElement : Container
    {
        public override SchemaType Type => Record.Type;

        public RootElement(IContainer parent, SchemaRecord schemaRecord, ImmutableJson json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, transaction)
        {
            this.SchemaRecord = schemaRecord;
            this.originalJson = originalJson;
            this.Record = new RecordElement(this, new RecordSchemaType(schemaRecord), json, originalJson, transaction);
            if (!Record.RecordType.AllFields.Any())
                FieldCaptionWidth = 100;
            else
                FieldCaptionWidth = Record.FieldCaptionWidth;

            if (originalJson != null && originalJson.IsObject)
                originalSchemalessFieldsCount = originalJson.AsObject.Keys.Except(SchemaRecord.AllFields.Select(f => f.Name)).Count();

            InitSchemalessFields(json, transaction);
        }

        public RecordElement Record { get; }

        public SchemaRecord SchemaRecord { get; }

        public double FieldCaptionWidth { get; }

        private readonly List<SchemalessField> schemalessFields = new();

        public IReadOnlyList<SchemalessField> SchemalessFields => schemalessFields;

        private int originalSchemalessFieldsCount;

        public override ImmutableJson Json
        {
            get
            {
                var json = Record.Json.AsObject.ToMutable();
                if (SchemaRecord.Parent != null)
                    json[SchemaRecord.Parent.Tag] = SchemaRecord.TagValue!;
                foreach (var schemalessField in schemalessFields)
                {
                    json[schemalessField.Name] = schemalessField.Json;
                }
                return json;
            }
        }

        private void InitSchemalessFields(ImmutableJson? json, ITransaction transaction)
        {
            if (json != null && json.IsObject)
            {
                var obj = new JsonObject(json.AsObject);
                foreach (var field in SchemaRecord.AllFields)
                {
                    obj.Remove(field.Name);
                }
                foreach (var pair in obj.OrderBy(pair => pair.Key))
                {
                    ImmutableJson? originalValue = null;
                    if (ReferenceEquals(json, OriginalJson))
                        originalValue = pair.Value;
                    else if (OriginalJson != null && OriginalJson.IsObject)
                        OriginalJson.AsObject.TryGetValue(pair.Key, out originalValue);

                    var field = new SchemalessField(this, pair.Key, pair.Value, originalValue, transaction);
                    schemalessFields.Add(field);
                }
            }
        }

        public override ImmutableJson? OriginalJson => originalJson;
        private ImmutableJson? originalJson;

        public override Element GetByPath(JsonPath path)
        {
            var result = Record.GetByPath(path);
            if (result == Record)
            {
                result = this;
                var head = path.Head;
                if (head is JsonObjectKeyPathNode objectKey)
                {
                    var field = this.SchemalessFields.FirstOrDefault(p => p.Name == objectKey.Key);
                    if (field != null)
                        result = field;
                }
                else if (head is JsonObjectPathNode key)
                {
                    var field = this.SchemalessFields.FirstOrDefault(p => p.Name == key.Key);
                    if (field != null)
                        result = field.Element.GetByPath(path.Tail);
                }
            }
            return result;
        }

        public override void SetOriginalJson(ImmutableJson? newOriginalJson, ITransaction transaction)
        {
            originalJson = newOriginalJson;
            Record.SetOriginalJson(newOriginalJson, transaction);
            foreach (var schemalessField in SchemalessFields)
            {
                schemalessField.SetOriginalJson(SafeJson.GetField(newOriginalJson, schemalessField.Name), transaction);
            }
            if (OriginalJson != null && OriginalJson.IsObject)
                originalSchemalessFieldsCount = OriginalJson.AsObject.Keys.Except(SchemaRecord.AllFields.Select(f => f.Name)).Count();
        }

        public override void SetJson(ImmutableJson? json, ITransaction transaction)
        {
            if (json != null && json.IsObject)
            {
                Record.SetJson(json, transaction);
                var obj = json.AsObject.ToMutable();
                foreach (var child in SchemalessFields.ToList())
                {
                    if (!obj.ContainsKey(child.Name))
                    {
                        RemoveSchemalessField(child, transaction);
                    }
                    else
                    {
                        child.SetJson(json.GetValueSafe(child.Name), transaction);
                        obj.Remove(child.Name);
                    }
                }
                foreach (var field in SchemaRecord.AllFields)
                {
                    obj.Remove(field.Name);
                }

                foreach (var pair in obj.OrderBy(pair => pair.Key))
                {
                    var field = new SchemalessField(this, pair.Key, pair.Value, null, transaction);
                    AddSchemalessField(field, transaction);
                }
            }
            else
                transaction.AddWarning(Path, "Invalid value");
        }

        protected override bool IsOverriden(ImmutableJson? baseJson)
        {
            return Record.Inherit(baseJson);
        }

        private void InsertSchemalessField(SchemalessField field)
        {
            for (int i = 0; i < schemalessFields.Count; i++)
            {
                if (string.Compare(schemalessFields[i].Name, field.Name, StringComparison.Ordinal) > 0)
                {
                    schemalessFields.Insert(i, field);
                    return;
                }
            }
            schemalessFields.Add(field);
        }

        public void AddSchemalessField(string name, ImmutableJson data, ITransaction transaction)
        {
            ImmutableJson? originalValue = null;
            if (OriginalJson != null && OriginalJson.IsObject)
            {
                OriginalJson.AsObject.TryGetValue(name, out originalValue);
            }
            var field = new SchemalessField(this, name, data, originalValue, transaction);
            AddSchemalessField(field, transaction);
        }

        private void AddSchemalessField(SchemalessField field, ITransaction transaction)
        {
            void Undo(ITransaction t)
            {
                schemalessFields.Remove(field);
                ValueChanged(t);
            }

            void Redo(ITransaction t)
            {
                InsertSchemalessField(field);
                ValueChanged(t);
            }

            transaction.AddUndoRedo(Undo, Redo);
            transaction.RefreshPresentation();
            InsertSchemalessField(field);
            ValueChanged(transaction);
        }

        public void RemoveSchemalessField(SchemalessField field, ITransaction transaction)
        {
            void Undo(ITransaction t)
            {
                ValueChanged(t);
                InsertSchemalessField(field);
            }

            void Redo(ITransaction t)
            {
                ValueChanged(t);
                schemalessFields.Remove(field);
            }

            transaction.AddUndoRedo(Undo, Redo);
            transaction.RefreshPresentation();
            schemalessFields.Remove(field);
            ValueChanged(transaction);
        }

        public override void Present(PresentationContext context)
        {
            foreach (var child in Record.Children)
            {
                child.Present(context);
            }

            foreach (var child in schemalessFields)
            {
                child.Present(context);
            }
        }

        public override IEnumerable<Element> GetChildren()
        {
            yield return Record;
            foreach (var schemalessField in schemalessFields)
                yield return schemalessField;
        }

        protected override void Validate(bool invalidateChildren, ITransaction transaction, out bool isValid, out bool isModified)
        {
            base.Validate(invalidateChildren, transaction, out isValid, out isModified);
            isModified = isModified || originalSchemalessFieldsCount != schemalessFields.Count;
        }
    }
}
