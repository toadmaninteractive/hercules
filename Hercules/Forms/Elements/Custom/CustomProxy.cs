using Hercules.Forms.Schema;
using Json;
using System.Windows.Input;

namespace Hercules.Forms.Elements
{
    public abstract class CustomProxy<T> : ProxyElement where T : CustomSchemaType
    {
        public T CustomType { get; }

        public override SchemaType Type => CustomType;

        protected CustomProxy(IContainer parent, T customType, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, transaction)
        {
            CustomType = customType;
            this.Element = Form.Factory.Create(this, customType.ContentType, json, originalJson, transaction);
        }
    }

    public abstract class EditableCustomProxy<T> : CustomProxy<T> where T : CustomSchemaType
    {
        public ICommand EditCommand { get; }

        public bool IsExpanded
        {
            get => GetFlag(ElementFlags.Expanded);
            set
            {
                if (SetFlag(ElementFlags.Expanded, value))
                {
                    Form.RefreshPresentation();
                }
            }
        }

        protected EditableCustomProxy(IContainer parent, T customType, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, customType, json, originalJson, transaction)
        {
            this.Element = Form.Factory.CreateNotOptional(this, customType.ContentType, json, originalJson, transaction);
            this.EditCommand = Commands.Execute(Edit);
        }

        protected virtual void Edit()
        {
        }
    }
}