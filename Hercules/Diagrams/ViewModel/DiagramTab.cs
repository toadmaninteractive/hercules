using Hercules.Documents;
using Hercules.Documents.Editor;
using Hercules.Forms.Schema;
using Hercules.Shell;
using System.Windows.Input;

namespace Hercules.Diagrams
{
    /// <summary>
    /// ViewModel for DiagramView
    /// </summary>
    public class DiagramTab : PageTab
    {
        public ICommand CopyCommand { get; }
        public ICommand CutCommand { get; }
        public ICommand PasteCommand { get; }
        public ICommand DeleteCommand { get; }

        public DocumentEditorPage Editor { get; }
        public ToolBoxTool ToolBoxTool { get; }
        private readonly PropertyEditorTool propertyEditorTool;

        private SchemafulDatabase schemafulDatabase;
        private SchemaDiagram schemaDiagram = default!;

        private DiagramViewModel diagram = default!;

        public DiagramViewModel Diagram
        {
            get => diagram;
            private set => SetField(ref diagram, value);
        }

        public DiagramTab(DocumentEditorPage editor, ToolBoxTool toolBoxTool, PropertyEditorTool propertyEditorTool)
        {
            this.Title = "Diagram";
            this.Editor = editor;
            this.ToolBoxTool = toolBoxTool;
            this.propertyEditorTool = propertyEditorTool;
            this.schemafulDatabase = Editor.SchemafulDatabase;

            void Cut()
            {
                var copy = Diagram.CopySelection();
                Diagram.DeleteSelection();
                ClipboardHelper.SetJson(copy);
            }

            CopyCommand = Commands.Execute(() => ClipboardHelper.SetJson(Diagram.CopySelection()));
            CutCommand = Commands.Execute(Cut);
            PasteCommand = Commands.Execute(() => Diagram.PasteSelection(ClipboardHelper.GetJson())).If(() => ClipboardHelper.GetJson() != null);
            DeleteCommand = Commands.Execute(() => Diagram.DeleteSelection());

            RoutedCommandBindings.Add(ApplicationCommands.Undo, () => Editor.FormTab.Form.Undo(), () => Editor.FormTab.Form.History.CanUndo);
            RoutedCommandBindings.Add(ApplicationCommands.Redo, () => Editor.FormTab.Form.Redo(), () => Editor.FormTab.Form.History.CanRedo);
            RoutedCommandBindings.Add(ApplicationCommands.Copy, CopyCommand);
            RoutedCommandBindings.Add(ApplicationCommands.Cut, CutCommand);
            RoutedCommandBindings.Add(ApplicationCommands.Paste, PasteCommand);
            RoutedCommandBindings.Add(ApplicationCommands.Delete, DeleteCommand);

            ApplySchema();
        }

        public void FillToolBox()
        {
            ToolBoxTool.Refresh(schemaDiagram.Palette);
        }

        public void ApplySchema()
        {
            this.schemafulDatabase = Editor.SchemafulDatabase;
            this.schemaDiagram = schemafulDatabase.DiagramSchema.GetDiagram(Editor.Category.Name);
            var properties = new ElementProperties(Editor, null);
            Diagram = new DiagramViewModel(Editor.FormTab.Form, schemaDiagram, properties);
            if (Editor.IsActive)
                FillToolBox();
        }

        public override void OnActivate()
        {
            Editor.Properties.Value = Diagram.Properties;
            FillToolBox();
            propertyEditorTool.Show();
            ToolBoxTool.Show();
        }

        public override void OnDeactivate()
        {
            ToolBoxTool.ToolBoxItems.Clear();
        }
    }
}