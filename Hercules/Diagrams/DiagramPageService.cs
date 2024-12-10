using Hercules.Documents;
using Hercules.Documents.Editor;
using Hercules.Forms.Schema;
using Hercules.Shell;

namespace Hercules.Diagrams
{
    public class DiagramPageService : IOnApplySchema
    {
        private readonly PropertyEditorTool propertyEditorTool;

        public DocumentEditorPage Editor { get; }
        public ToolBoxTool ToolBoxTool { get; }

        public DiagramTab? Diagram { get; private set; }

        public DiagramPageService(DocumentEditorPage documentEditor, ToolBoxTool toolBoxTool, PropertyEditorTool propertyEditorTool)
        {
            Editor = documentEditor;
            ToolBoxTool = toolBoxTool;
            this.propertyEditorTool = propertyEditorTool;
            SetupDiagram(documentEditor.SchemafulDatabase, documentEditor.Category);
        }

        public void OnApplySchema(SchemafulDatabase schemafulDatabase, Category category, SchemaRecord schemaRecord)
        {
            SetupDiagram(schemafulDatabase, category);
        }

        void SetupDiagram(SchemafulDatabase schemafulDatabase, Category category)
        {
            if (schemafulDatabase.DiagramSchema != null && category.IsSchemaful && schemafulDatabase.DiagramSchema.IsDiagramEnabled(category.Name))
            {
                if (Diagram == null)
                {
                    this.Diagram = new DiagramTab(Editor, ToolBoxTool, propertyEditorTool);
                    Editor.Tabs.Insert(0, Diagram);
                }
                else
                    Diagram.ApplySchema();
                Editor.ActiveTab = Diagram;
            }
            else if (Diagram != null)
            {
                if (Editor.ActiveTab == Diagram)
                    Editor.GoTo(DestinationTab.Form);
                Diagram.OnClose();
                Editor.Tabs.Remove(Diagram);
                Diagram = null;
            }
        }
    }
}