using Hercules.Forms.Elements;
using Hercules.Forms.Schema;
using Json;
using System.Collections.Generic;
using System.Linq;

namespace Hercules.Documents
{
    public abstract class CustomTypeSupport
    {
        private readonly FormSchema editorMetaSchema;

        protected CustomTypeSupport(FormSchema editorMetaSchema)
        {
            this.editorMetaSchema = editorMetaSchema;
        }

        public abstract string Tag { get; }

        public abstract CustomSchemaType CreateSchemaType(IDocument editorDocument, SchemaType contentType, bool optional = false, string? help = null);

        public abstract Element CreateElement(IContainer parent, CustomSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction, ElementFactoryContext context);

        public virtual Element CreateKeyElement(IContainer parent, CustomSchemaType type, string jsonKey, string? originalJsonKey, ITransaction transaction, ElementFactoryContext context) =>
            new InvalidElement(parent, jsonKey, originalJsonKey == null ? null : ImmutableJson.Create(originalJsonKey), transaction);

        public abstract DocumentDraft CreateEditorDraft(string documentId, TempStorage tempStorage);

        public virtual SchemaRecord EditorMetaSchemaRecord => editorMetaSchema.Variant.GetChild(Tag);
    }

    public class CustomTypeRegistry
    {
        private readonly List<CustomTypeSupport> types = new List<CustomTypeSupport>();

        public void Register(CustomTypeSupport customTypeSupport)
        {
            types.Add(customTypeSupport);
        }

        public CustomTypeSupport? Get(string tag) => types.FirstOrDefault(editor => editor.Tag == tag);

        public IReadOnlyList<CustomTypeSupport> All => types;
    }
}