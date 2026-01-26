using Hercules.Forms.Presentation;
using Hercules.Forms.Schema;
using ICSharpCode.AvalonEdit.Document;
using Json;
using System;
using System.ComponentModel;

namespace Hercules.Forms.Elements
{
    public class JsonElement : SimpleElement<string>
    {
        public override SchemaType Type { get; }

        public JsonElement(IContainer parent, JsonSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, originalJson, transaction)
        {
            this.Type = type;
            this.value = (json ?? ImmutableJson.Null).ToString(JsonFormat.Multiline);
            Document = new TextDocument(value);
            Document.TextChanged += Document_TextChanged;
            Document.PropertyChanged += Document_PropertyChanged;
            HasPostUpdate = true;
        }

        private void Document_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TextDocument.LineCount))
            {
                Form.RefreshPresentation();
                RaisePropertyChanged(nameof(Height));
            }
        }

        void Document_TextChanged(object? sender, EventArgs e)
        {
            Value = Document.Text;
        }

        protected override void PostUpdate()
        {
            if (Document.Text != Value)
                Document.Text = Value;
        }

        public TextDocument Document { get; }

        protected override void Validate(bool invalidateChildren, ITransaction transaction, out bool isValid, out bool isModified)
        {
            try
            {
                var json = JsonParser.Parse(Value);
                isValid = true;
                isModified = !ImmutableJson.Equals(OriginalJson, json);
            }
            catch (JsonParserException)
            {
                isValid = false;
                isModified = true;
            }
        }

        protected override bool IsOverriden(ImmutableJson? baseJson)
        {
            try
            {
                var json = JsonParser.Parse(Value);
                return !ImmutableJson.Equals(baseJson, json);
            }
            catch (JsonParserException)
            {
                return false;
            }
        }

        public override ImmutableJson Json
        {
            get
            {
                try
                {
                    return JsonParser.Parse(Value);
                }
                catch (JsonParserException)
                {
                    return ImmutableJson.Null;
                }
            }
        }

        public override string JsonKey => throw new NotSupportedException();

        public override void SetJson(ImmutableJson? json, ITransaction transaction)
        {
            bool same;
            try
            {
                var oldJson = JsonParser.Parse(Value);
                same = oldJson.Equals(json);
            }
            catch (JsonParserException)
            {
                same = false;
            }
            if (!same)
            {
                SetValue(json == null ? "null" : json.ToString(JsonFormat.Multiline), transaction);
            }
        }

        public double Height => Math.Max(20, Math.Min(4 + Document.LineCount * 14, 300));

        public override void Present(PresentationContext context)
        {
            var proxy = context.GetProxy(this);
            context.AddItem(proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool(GetType()), isTabStop: true, dock: context.FillDock), height: Height);
        }
    }
}
