using Hercules.Documents.Editor;
using Hercules.Shell;
using System;

namespace Hercules.Documents
{
    public class SchemaUpdater
    {
        private readonly Workspace workspace;
        private readonly IReadOnlyObservableValue<SchemaUpdateType> schemaUpdateType;

        private const string SchemaUpdateAdviceType = "SchemaUpdate";

        public SchemaUpdater(Workspace workspace, IReadOnlyObservableValue<SchemaUpdateType> schemaUpdateType)
        {
            this.workspace = workspace;
            this.schemaUpdateType = schemaUpdateType;
        }

        private void AdviceSchemaUpdate(Action applySchemaUpdate)
        {
            workspace.AdviceManager.RemoveByType(SchemaUpdateAdviceType);
            var command = Commands.Execute(() => { applySchemaUpdate(); workspace.AdviceManager.RemoveByType(SchemaUpdateAdviceType); });
            workspace.AdviceManager.AddAdvice(SchemaUpdateAdviceType, "Apply schema update", command);
        }

        public void Close()
        {
            workspace.AdviceManager.RemoveByType(SchemaUpdateAdviceType);
        }

        public void SchemaUpdateAvailable(Action applySchemaUpdate)
        {
            switch (schemaUpdateType.Value)
            {
                case SchemaUpdateType.Silent:
                    applySchemaUpdate();
                    break;
                case SchemaUpdateType.Confirmation:
                    if (workspace.DialogService.ShowQuestion("Schema update is available. Apply?\n\n(You can use notifications menu to apply it later)"))
                        applySchemaUpdate();
                    else
                        AdviceSchemaUpdate(applySchemaUpdate);
                    break;
                case SchemaUpdateType.Notification:
                    AdviceSchemaUpdate(applySchemaUpdate);
                    workspace.AdviceManager.NewAdvice = true;
                    break;
            }
        }
    }
}
