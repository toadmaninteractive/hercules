using Hercules.Forms.Presentation;
using Hercules.Forms.Schema;
using Json;
using System.Linq;
using System.Windows.Input;

namespace Hercules.Forms.Elements
{
    public class LocalizedElement : ProxyElement
    {
        public override SchemaType Type => LocalizedType;

        public LocalizedSchemaType LocalizedType { get; }

        public LocalizedElement(IContainer parent, LocalizedSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, transaction)
        {
            this.LocalizedType = type;

            ImmutableJson actualJson = json ?? ImmutableJson.Null;

            Record = new RecordElement(this, LocalizedType.RecordType, actualJson, originalJson, transaction);
            Record.SetExpandedStateRecursively(true, transaction);

            this.Element = Record;
            Text = Record.GetByPath(JsonPath.Parse("text"));
            ApprovedText = Record.Children.FirstOrDefault(f => f.Name == "approved_text")?.DeepElement;

            if (ApprovedText != null)
            {
                ApproveCommand = Commands.Execute(Approve).If(() => !isApproved);
                IsApproved = ImmutableJson.Equals(ApprovedText?.Json ?? EmptyStringJson, Text.Json);
            }
        }

        private static readonly ImmutableJson EmptyStringJson = "";

        protected override void Validate(bool invalidateChildren, ITransaction transaction, out bool isValid, out bool isModified)
        {
            base.Validate(invalidateChildren, transaction, out isValid, out isModified);
            if (ApprovedText != null)
            {
                IsApproved = ImmutableJson.Equals(ApprovedText?.Json ?? EmptyStringJson, Text.Json);
            }
        }

        public RecordElement Record { get; }

        public Element Text { get; }
        public Element? ApprovedText { get; }

        private bool isApproved;

        public bool IsApproved
        {
            get => isApproved;
            private set => SetField(ref isApproved, value);
        }

        public ICommand? ApproveCommand { get; }

        public override Element GetByPath(JsonPath path)
        {
            if (path.Head == null)
                return Text;
            else
                return Record.GetByPath(path);
        }

        bool recordView;

        public bool RecordView
        {
            get => recordView;
            set
            {
                if (SetField(ref recordView, value))
                {
                    Form.RefreshPresentation();
                }
            }
        }

        public override void Present(PresentationContext context)
        {
            var proxy = context.GetProxy(this);
            var left = context.Left;
            Text.Present(context);
            if (ApprovedText != null)
            {
                context.AddItem(proxy.Item1 ??= new VirtualRowItem(this, ControlPools.GetPool("ApprovedElement"), dock: context.RightDock, width: 24));
            }
            context.AddItem(proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool("LocalizedElement"), dock: context.RightDock, width: 24));
            if (RecordView)
            {
                context.Indent(left);
                foreach (var child in Record.Children)
                {
                    if (child.Name != "text")
                    {
                        child.Present(context);
                    }
                }
                context.Outdent();
            }
        }

        protected override void OnValueUpdate(ITransaction transaction)
        {
            base.OnValueUpdate(transaction);
            IsApproved = ImmutableJson.Equals(ApprovedText?.Json ?? EmptyStringJson, Text.Json);
        }

        private void Approve()
        {
            Form.Run(t => ApprovedText?.SetJson(Text.Json, t));
        }
    }
}