using Hercules.Documents;
using Hercules.Shell;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace Hercules.DatabaseExplorer
{
    public class DatabaseExplorerTree : NotifyPropertyChanged
    {
        public CompositeCollection Items { get; }
        public List<DatabaseExplorerCategory> Categories { get; }
        public IReadOnlyDictionary<string, DatabaseExplorerCategoryGroup> CategoryGroups { get; }
        public DatabaseExplorerSchemalessDocuments SchemalessCategory { get; }
        public DatabaseExplorerSchemalessDocuments DesignCategory { get; }
        public DatabaseExplorerSchemalessDocuments SchemaCategory { get; }
        public DatabaseExplorerSchemalessDocuments ScriptsCategory { get; }
        public HashSet<string> CollapsedCategories { get; }
        public bool GroupByCategories
        {
            get => groupByCategories;
            set
            {
                if (groupByCategories != value)
                {
                    groupByCategories = value;
                    Invalidate();
                    TreeItemsRefresh();
                }
            }
        }
        public string Filter
        {
            get => filter;
            set
            {
                if (filter != value)
                {
                    filter = value;
                    RaisePropertyChanged();
                    Invalidate();
                }
            }
        }
        public WorkspaceContextMenu? DocumentMenu { get; }
        public WorkspaceContextMenu? CategoryMenu { get; }
        public Visibility CategoryGroupsVisibility
        {
            get => categoryGroupsVisibility;
            set => SetField(ref categoryGroupsVisibility, value);
        }

        private string filter;
        private bool groupByCategories = true;
        private Visibility categoryGroupsVisibility;

        public DatabaseExplorerTree(SchemafulDatabase schemafulDatabase, HashSet<string> collapsedCategories, string filter, WorkspaceContextMenu? documentMenu, WorkspaceContextMenu? categoryMenu)
        {
            this.filter = filter;
            this.CollapsedCategories = collapsedCategories;
            this.Categories = new List<DatabaseExplorerCategory>();
            this.Items = new CompositeCollection();
            this.DocumentMenu = documentMenu;
            this.CategoryMenu = categoryMenu;
            this.SchemalessCategory = new DatabaseExplorerSchemalessDocuments(schemafulDatabase.SchemalessDocuments.Documents, this, "no category", "_schemaless");
            this.SchemaCategory = new DatabaseExplorerSchemalessDocuments(schemafulDatabase.SchemaDocuments.Documents, this, "schema", "_schema");
            this.ScriptsCategory = new DatabaseExplorerSchemalessDocuments(schemafulDatabase.ScriptDocuments.Documents, this, "script", "_script");
            this.DesignCategory = new DatabaseExplorerSchemalessDocuments(schemafulDatabase.DesignDocuments.Documents, this, "design", "_design");
            CategoryGroups = schemafulDatabase.CategoryGroupNames.ToDictionary(x => x, x => new DatabaseExplorerCategoryGroup(this, !CollapsedCategories.Contains(x), x));
            CategoryGroupsVisibility = CategoryGroups.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            InitializationCategoriesAndGroups(schemafulDatabase.Categories);
            TreeItemsRefresh();
        }

        public void Invalidate()
        {
            foreach (var category in Categories)
                category.Invalidate();

            foreach (var categoryGroup in CategoryGroups)
                foreach (var category in categoryGroup.Value.Categories)
                {
                    category.IsWithoutGroup = !groupByCategories;
                    category.Invalidate();
                }

            SchemalessCategory.Invalidate();
        }

        /// <summary>
        /// Update items in Tree view according to Categories and CategoryGroups
        /// </summary>
        private void TreeItemsRefresh()
        {
            SortedDictionary<string, CollectionContainer> explorerItems = new SortedDictionary<string, CollectionContainer>();
            foreach (var category in Categories)
                explorerItems.Add(category.Name, new CollectionContainer { Collection = category.Items });

            if (!GroupByCategories)
            {
                foreach (var categoryGroup in CategoryGroups)
                    foreach (var category in categoryGroup.Value.Categories)
                        explorerItems.Add(category.Category.Name, new CollectionContainer { Collection = category.Items });
            }
            else
            {
                foreach (var categoryGroup in CategoryGroups)
                    explorerItems.Add(categoryGroup.Key, new CollectionContainer { Collection = categoryGroup.Value.Items });
            }

            Items.Clear();
            foreach (var item in explorerItems)
                Items.Add(item.Value);
            Items.Add(new CollectionContainer { Collection = SchemalessCategory.Items });
            Items.Add(new CollectionContainer { Collection = SchemaCategory.Items });
            Items.Add(new CollectionContainer { Collection = ScriptsCategory.Items });
            Items.Add(new CollectionContainer { Collection = DesignCategory.Items });
        }

        /// <summary>
        /// List categories and category groups initializations
        /// </summary>
        private void InitializationCategoriesAndGroups(IReadOnlyList<Category> categories)
        {
            foreach (var category in categories)
            {
                var categoryView = new DatabaseExplorerCategory(category, this);
                if (category.Group != null)
                {
                    CategoryGroups[category.Group].AddCategory(categoryView);
                }
                else
                {
                    Categories.Add(categoryView);
                }
            }
        }

        public void ExpandAll()
        {
            foreach (var category in Categories)
                category.IsExpanded = true;

            foreach (var categoryGroup in CategoryGroups)
                categoryGroup.Value.IsExpanded = true;

            SchemaCategory.IsExpanded = true;
            SchemalessCategory.IsExpanded = true;
            DesignCategory.IsExpanded = true;
            ScriptsCategory.IsExpanded = true;
        }

        public void CollapseAll()
        {
            foreach (var category in Categories)
                category.IsExpanded = false;

            foreach (var categoryGroup in CategoryGroups)
                categoryGroup.Value.IsExpanded = false;

            SchemaCategory.IsExpanded = false;
            SchemalessCategory.IsExpanded = false;
            DesignCategory.IsExpanded = false;
            ScriptsCategory.IsExpanded = false;
        }
    }
}