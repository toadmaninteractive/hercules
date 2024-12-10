using Hercules.Shell;

namespace Hercules.Documents.Editor
{
    public class SchemaUpdateSettingsTab : PageTab
    {
        public Setting<SchemaUpdateType> SchemaUpdate { get; }
        public Setting<bool> AskSchemaUpdateConfirmationForModified { get; }

        public SchemaUpdateSettingsTab(Setting<SchemaUpdateType> schemaUpdate, Setting<bool> askSchemaUpdateConfirmationForModified)
        {
            this.SchemaUpdate = schemaUpdate;
            this.AskSchemaUpdateConfirmationForModified = askSchemaUpdateConfirmationForModified;
            this.Title = "Schema Update";
        }
    }
}
