using Hercules.Documents;
using Hercules.Repository;
using Hercules.Shell;
using Hercules.Shortcuts;
using Json;

namespace Hercules.Forms.Schema
{
    public interface IFormSchemaFactory
    {
        FormSchema CreateFormSchema(ImmutableJson json, SchemafulDatabase? schemafulDatabase);
    }

    public sealed record FormSchemaFactory(FormSettings FormSettings, TextSizeService TextSizeService, ProjectSettings? ProjectSettings, ShortcutService ShortcutService, CustomTypeRegistry CustomTypeRegistry) : IFormSchemaFactory
    {
        public FormSchema CreateFormSchema(ImmutableJson json, SchemafulDatabase? schemafulDatabase)
        {
            return new IgorFormSchemaBuilder(json, FormSettings, ProjectSettings, TextSizeService, ShortcutService, CustomTypeRegistry, schemafulDatabase).FormSchema;
        }
    }
}