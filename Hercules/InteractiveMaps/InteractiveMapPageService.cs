using Hercules.Documents;
using Hercules.Documents.Editor;
using Hercules.Forms.Schema;
using System.Linq;

namespace Hercules.InteractiveMaps
{
    public class InteractiveMapPageService : IOnApplySchema
    {
        public DocumentEditorPage Editor { get; }

        public InteractiveMapPageService(DocumentEditorPage documentEditor)
        {
            Editor = documentEditor;
        }

        public void OnApplySchema(SchemafulDatabase schemafulDatabase, Category category, SchemaRecord schemaRecord)
        {
            var tabsToClose = Editor.Tabs.OfType<InteractiveMapTab>().ToList();
            Editor.GoTo(DestinationTab.Form);

            foreach (var tab in tabsToClose)
                Editor.Tabs.Remove(tab);
        }
    }
}