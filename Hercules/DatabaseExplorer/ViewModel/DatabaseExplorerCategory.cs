using Hercules.Documents;
using Hercules.Shell;

namespace Hercules.DatabaseExplorer
{
    public interface ICategoryContextMenu
    {
        Category Category { get; }
        string Name { get; }
    }

    public class DatabaseExplorerCategory : DatabaseExplorerDocumentGroup, ICategoryContextMenu
    {
        protected override void OnExpandedChanged()
        {
            if (IsExpanded)
                Explorer.CollapsedCategories.Remove(Category.Name);
            else
                Explorer.CollapsedCategories.Add(Category.Name);
        }

        public Category Category { get; }
        public string Name { get; }

        public DatabaseExplorerCategory(Category category, DatabaseExplorerTree explorer)
            : base(category.Documents, explorer, !explorer.CollapsedCategories.Contains(category.Name))
        {
            this.Category = category;
            this.Name = category.Name;
        }

        public override WorkspaceContextMenu? ContextMenu => Explorer.CategoryMenu;
    }
}
