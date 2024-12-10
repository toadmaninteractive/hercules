using Hercules.Documents;
using Hercules.Shell;
using System.Collections.Generic;
using System.Windows.Data;

namespace Hercules.DatabaseExplorer
{
    public class DatabaseExplorerCategoryGroup : DatabaseExplorerItem, ICategoryContextMenu
    {
        public string Name { get; }
        public override WorkspaceContextMenu? ContextMenu => Explorer.CategoryMenu;
        public DatabaseExplorerTree Explorer { get; }
        public CompositeCollection Items { get; } = new();
        public List<DatabaseExplorerCategory> Categories { get; } = new();
        public Category Category => Categories[0].Category;
        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                if (isExpanded != value)
                {
                    isExpanded = value;
                    foreach (var documentGroup in Categories)
                    {
                        documentGroup.InExpandGroup = value;
                        documentGroup.IsExpanded = value;
                        documentGroup.Invalidate();
                    }

                    RaisePropertyChanged();
                    OnExpandedChanged();
                }
            }
        }

        private bool isExpanded;

        public DatabaseExplorerCategoryGroup(DatabaseExplorerTree explorer, bool isInitiallyExpanded, string name)
        {
            Explorer = explorer;
            isExpanded = isInitiallyExpanded;
            Name = name;

            Items.Add(this);
        }

        public void AddCategory(DatabaseExplorerCategory categoryView)
        {
            Categories.Add(categoryView);
            categoryView.InExpandGroup = isExpanded;
            categoryView.IsWithoutGroup = false;
            categoryView.Invalidate();
            Items.Add(new CollectionContainer() { Collection = categoryView.Items });
        }

        protected virtual void OnExpandedChanged()
        {
            if (IsExpanded)
                Explorer.CollapsedCategories.Remove(Name);
            else
                Explorer.CollapsedCategories.Add(Name);
        }
    }
}