using Hercules.Documents.Editor;
using Hercules.Shell;
using System.Windows.Input;

namespace Hercules.InteractiveMaps
{
    /// <summary>
    /// ViewModel for InteractiveMapView
    /// </summary>
    public class InteractiveMapTab : PageTab
    {
        public DocumentEditorPage Editor { get; }

        private InteractiveMapViewModel viewModel;
        private readonly InteractiveMapElement element;
        private readonly PropertyEditorTool propertyEditorTool;

        public InteractiveMapViewModel ViewModel
        {
            get => viewModel;
            set => SetField(ref viewModel, value);
        }

        public InteractiveMapTab(DocumentEditorPage editor, InteractiveMapElement element, PropertyEditorTool propertyEditorTool)
        {
            Title = element.Editor.Title;
            Editor = editor;
            this.element = element;
            this.propertyEditorTool = propertyEditorTool;

            RoutedCommandBindings.Add(ApplicationCommands.Undo, () => Editor.FormTab.Form.Undo(), () => Editor.FormTab.Form.History.CanUndo);
            RoutedCommandBindings.Add(ApplicationCommands.Redo, () => Editor.FormTab.Form.Redo(), () => Editor.FormTab.Form.History.CanRedo);

            ApplyDataFromElement();
        }

        public void ApplyDataFromElement()
        {
            ViewModel = new InteractiveMapViewModel(Editor, element);
        }

        public override void OnActivate()
        {
            propertyEditorTool.Show();
        }
    }
}