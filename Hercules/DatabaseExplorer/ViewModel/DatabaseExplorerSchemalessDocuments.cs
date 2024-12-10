using Hercules.Documents;
using Hercules.Shell;
using System.Collections.ObjectModel;

namespace Hercules.DatabaseExplorer
{
    public class DatabaseExplorerSchemalessDocuments : DatabaseExplorerDocumentGroup
    {
        public string Title { get; }

        private readonly string tag;

        protected override void OnExpandedChanged()
        {
            if (IsExpanded)
                Explorer.CollapsedCategories.Remove(tag);
            else
                Explorer.CollapsedCategories.Add(tag);
        }

        public DatabaseExplorerSchemalessDocuments(ObservableCollection<IDocument> documents, DatabaseExplorerTree explorer, string title, string tag)
            : base(documents, explorer, !explorer.CollapsedCategories.Contains(tag))
        {
            Title = title;
            this.tag = tag;
        }

        public override WorkspaceContextMenu? ContextMenu => null;
    }
}
