using Hercules.Documents;
using Hercules.Shell;
using System;

namespace Hercules.DatabaseExplorer
{
    public class DatabaseExplorerDocument : DatabaseExplorerItem, IComparable<DatabaseExplorerDocument>
    {
        public IDocument Document { get; }
        public string FilterPrefix
        {
            get => filterPrefix;
            set => SetField(ref filterPrefix, value);
        }
        public string FilterPattern
        {
            get => filterPattern;
            set => SetField(ref filterPattern, value);
        }
        public string FilterSuffix
        {
            get => filterSuffix;
            set => SetField(ref filterSuffix, value);
        }
        public string DocumentId { get; }
        public override WorkspaceContextMenu? ContextMenu { get; }

        private string filterPrefix = string.Empty;
        private string filterPattern = string.Empty;
        private string filterSuffix = string.Empty;

        public DatabaseExplorerDocument(IDocument document, WorkspaceContextMenu? documentMenu)
        {
            this.DocumentId = document.DocumentId;
            this.ContextMenu = documentMenu;
            this.FilterPrefix = document.DocumentId;
            this.Document = document;
        }

        public int CompareTo(DatabaseExplorerDocument? other)
        {
            if (other is null)
                return 1;
            return string.Compare(this.DocumentId, other.DocumentId, StringComparison.Ordinal);
        }

        public bool UpdateFilter(string filter)
        {
            if (string.IsNullOrEmpty(filter))
            {
                FilterPrefix = DocumentId;
                FilterPattern = string.Empty;
                FilterSuffix = string.Empty;
                return true;
            }
            else
            {
                var i = DocumentId.IndexOf(filter, StringComparison.OrdinalIgnoreCase);
                if (i < 0)
                {
                    return false;
                }
                else
                {
                    FilterPrefix = DocumentId.Substring(0, i);
                    FilterPattern = DocumentId.Substring(i, filter.Length);
                    FilterSuffix = DocumentId.Substring(i + filter.Length);
                    return true;
                }
            }
        }
    }
}
