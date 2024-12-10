using Hercules.Forms.Presentation;
using Hercules.Forms.Schema;
using Json;

namespace Hercules.Forms.Elements
{
    public class InvalidElement : Element
    {
        public override SchemaType Type { get; }

        ImmutableJson data;

        public InvalidElement(IContainer parent, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, transaction)
        {
            Type = new JsonSchemaType(); // TODO: invalid schema type
            this.originalJson = originalJson;
            data = json ?? ImmutableJson.Null;
        }

        public override ImmutableJson Json => data;

        public override string JsonKey => string.Empty;

        public override ImmutableJson? OriginalJson => originalJson;
        private ImmutableJson? originalJson;

        protected override void Validate(bool invalidateChildren, ITransaction transaction, out bool isValid, out bool isModified)
        {
            isValid = false;
            isModified = !ImmutableJson.Equals(OriginalJson, Json);
        }

        protected override bool IsOverriden(ImmutableJson? baseJson)
        {
            return !ImmutableJson.Equals(baseJson, Json);
        }

        void SetData(ImmutableJson json)
        {
            data = json;
        }

        public override void SetOriginalJson(ImmutableJson? newOriginalJson, ITransaction transaction)
        {
            originalJson = newOriginalJson;
        }

        public override void SetJson(ImmutableJson? json, ITransaction transaction)
        {
            if (!data.Equals(json))
            {
                ImmutableJson oldData = data;
                ImmutableJson newData = json ?? ImmutableJson.Null;
                data = newData;
                void Undo(ITransaction t)
                {
                    ValueChanged(t);
                    SetData(oldData);
                }

                void Redo(ITransaction t)
                {
                    ValueChanged(t);
                    SetData(newData);
                }

                transaction.AddUndoRedo(Undo, Redo);
                ValueChanged(transaction);
            }
        }

        public override void Present(PresentationContext context)
        {
            var proxy = context.GetProxy(this);
            context.AddItem(proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool(GetType()), width: 250), height: 22);
        }
    }
}
