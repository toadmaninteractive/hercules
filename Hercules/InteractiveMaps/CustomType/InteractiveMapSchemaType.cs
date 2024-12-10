using Hercules.Forms.Schema;
using Hercules.Forms.Schema.Custom;

namespace Hercules.InteractiveMaps
{
    public class InteractiveMapSchemaType : CustomSchemaType
    {
        public EditorInteractiveMap Editor { get; }

        public override string Tag => InteractiveMapCustomTypeSupport.TagValue;

        public InteractiveMapSchemaType(SchemaType contentType, EditorInteractiveMap editor, bool optional = false, string? help = null)
            : base(contentType, optional, help)
        {
            Editor = editor;
        }
    }
}