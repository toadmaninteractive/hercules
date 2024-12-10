using Hercules.Forms.Schema.Custom;

namespace Hercules.Forms.Schema
{
    public class BreadcrumbsSchemaType : CustomSchemaType
    {
        public EditorBreadcrumbs Editor { get; }

        public override string Tag => BreadcrumbsCustomTypeSupport.TagValue;

        public BreadcrumbsSchemaType(SchemaType detailType, EditorBreadcrumbs editor, bool optional = false, string? help = null)
            : base(detailType, optional, help)
        {
            this.Editor = editor;
        }

        public override bool Equals(SchemaType? other)
        {
            if (other is BreadcrumbsSchemaType that)
                return this.Kind == that.Kind && this.Optional == that.Optional && this.Help == that.Help && this.Editor == that.Editor;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return System.HashCode.Combine(base.GetHashCode(), Editor);
        }
    }
}