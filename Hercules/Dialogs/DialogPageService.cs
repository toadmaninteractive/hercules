using Hercules.Documents;
using Hercules.Documents.Editor;
using Hercules.Forms.Schema;
using Hercules.Shell;

namespace Hercules.Dialogs
{
    public class DialogPageService : IOnApplySchema
    {
        public DocumentEditorPage Editor { get; }
        public PropertyEditorTool PropertyEditorTool { get; }
        public DialogTab? Dialog { get; private set; }

        public DialogPageService(DocumentEditorPage documentEditor, PropertyEditorTool propertyEditorTool)
        {
            Editor = documentEditor;
            PropertyEditorTool = propertyEditorTool;
            SetupDiagram(documentEditor.Schema);
        }

        public void OnApplySchema(SchemafulDatabase schemafulDatabase, Category category, SchemaRecord schemaRecord)
        {
            SetupDiagram(schemaRecord);
        }

        void SetupDiagram(SchemaRecord schemaRecord)
        {
            if (Editor.Category.IsSchemaful && schemaRecord.IsDialog)
            {
                if (Dialog == null)
                {
                    this.Dialog = new DialogTab(Editor, PropertyEditorTool);
                    Editor.Tabs.Insert(0, Dialog);
                }
                else
                    Dialog.ApplySchema();
                Editor.ActiveTab = Dialog;
            }
            else if (Dialog != null)
            {
                if (Editor.ActiveTab == Dialog)
                    Editor.GoTo(DestinationTab.Form);
                Dialog.OnClose();
                Editor.Tabs.Remove(Dialog);
                Dialog = null;
            }
        }
    }
}