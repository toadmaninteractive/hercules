using Hercules.Forms;
using Hercules.Shell;

namespace Hercules.Documents.Editor
{
    public class DocumentEditorSettingsTab : PageTab
    {
        public FormSettings Settings { get; }

        public DocumentEditorSettingsTab(FormSettings settings)
        {
            this.Settings = settings;
            this.Title = "Document Editor";
        }
    }
}
