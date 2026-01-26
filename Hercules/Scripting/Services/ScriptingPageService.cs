using Hercules.Documents;
using Hercules.Documents.Editor;
using Hercules.Forms.Schema;

namespace Hercules.Scripting
{
    public class ScriptingPageService : IOnApplySchema
    {
        private readonly Project project;
        private readonly ScriptingModule scriptingModule;
        public DocumentEditorPage Editor { get; }

        public DocumentScriptTab? ScriptTab { get; private set; }

        public ScriptingPageService(DocumentEditorPage documentEditor, Project project, ScriptingModule scriptingModule)
        {
            this.project = project;
            this.scriptingModule = scriptingModule;
            Editor = documentEditor;
            SetupScriptTab(documentEditor.Category);
        }

        public void OnApplySchema(SchemafulDatabase schemafulDatabase, Category category, SchemaRecord schemaRecord)
        {
            SetupScriptTab(category);
        }

        void SetupScriptTab(Category category)
        {
            if (category.Type == CategoryType.Script)
            {
                if (ScriptTab == null)
                {
                    this.ScriptTab = new DocumentScriptTab(Editor, project, scriptingModule);
                    Editor.Tabs.Insert(0, ScriptTab);
                }
                Editor.ActiveTab = ScriptTab;
            }
            else if (ScriptTab != null)
            {
                if (Editor.ActiveTab == ScriptTab)
                    Editor.GoTo(DestinationTab.Form);
                ScriptTab.OnClose();
                Editor.Tabs.Remove(ScriptTab);
                ScriptTab = null;
            }
        }
    }
}
