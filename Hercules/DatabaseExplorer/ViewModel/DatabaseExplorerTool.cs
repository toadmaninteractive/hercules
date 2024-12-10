using Hercules.Documents;
using Hercules.Shell;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;

namespace Hercules.DatabaseExplorer
{
    public class DatabaseExplorerTool : Tool, IWorkspaceContextMenuProvider
    {
        public DatabaseExplorerTree? Tree
        {
            get => tree;
            set => SetField(ref tree, value);
        }
        public string Filter
        {
            get => filter;
            set
            {
                if (SetField(ref filter, value))
                {
                    if (!isUpdatingFilter)
                    {
                        isUpdatingFilter = true;
                        Dispatcher.CurrentDispatcher.BeginInvoke(new Action(UpdateFilter), DispatcherPriority.Input, null);
                    }
                }
            }
        }
        public IList? SelectedItems { get; set; }
        public ICommand ClearFilterCommand { get; }
        public HashSet<string> CollapsedCategories { get; } = new HashSet<string>();

        private DatabaseExplorerTree? tree;
        private readonly IDisposable schemafulDatabaseSubscription;
        private string filter = string.Empty;
        private bool isUpdatingFilter = false;

        public IReadOnlyObservableValue<Project?> ProjectObservable { get; }
        private readonly UiOptionManager optionManager;
        public DocumentsModule DocumentsModule { get; }

        public DatabaseExplorerTool(IReadOnlyObservableValue<Project?> projectObservable, DocumentsModule documentsModule, UiOptionManager optionManager)
        {
            Title = "Database Explorer";
            IsVisible = true;
            ContentId = "{DatabaseExplorer}";
            Pane = "LeftToolsPane";
            this.ProjectObservable = projectObservable;
            this.DocumentsModule = documentsModule;
            this.optionManager = optionManager;

            ClearFilterCommand = Commands.Execute(() => Filter = string.Empty);

            RoutedCommandBindings.Add(RoutedCommands.ExpandAll, () => Tree?.ExpandAll(), HasDatabase);
            RoutedCommandBindings.Add(RoutedCommands.CollapseAll, () => Tree?.CollapseAll(), HasDatabase);
            RoutedCommandBindings.Add(RoutedCommands.DuplicateItem, documentsModule.CloneDocumentCommand.For(GetActiveDocument));
            RoutedCommandBindings.Add(ApplicationCommands.Delete, documentsModule.DeleteDocumentCommand.Bulk.For(GetSelectedDocuments));
            schemafulDatabaseSubscription = projectObservable.Switch(p => p?.ObservableSchemafulDatabase).Subscribe(SchemafulDatabaseChanged);
        }

        public WorkspaceContextMenu? ContextMenu
        {
            get
            {
                if (SelectedItems != null && SelectedItems.Count == 1)
                    return ((DatabaseExplorerItem)SelectedItems[0]!).ContextMenu;
                else
                    return Tree?.DocumentMenu;
            }
        }

        public override object? GetCommandParameter(Type type)
        {
            if (type == typeof(Category))
                return GetActiveCategory();
            if (type == typeof(IDocument))
                return GetActiveDocument();
            if (type == typeof(IReadOnlyCollection<IDocument>))
                return GetSelectedDocuments();
            return base.GetCommandParameter(type);
        }

        public IDocument? GetActiveDocument()
        {
            if (SelectedItems == null)
                return null;
            var docs = SelectedItems.OfType<DatabaseExplorerDocument>();
            var count = docs.Count();
            if (count == 1)
                return docs.First().Document;
            else
                return null;
        }

        public Category? GetActiveCategory()
        {
            if (SelectedItems == null)
                return null;
            var docs = SelectedItems.OfType<ICategoryContextMenu>();
            var count = docs.Count();
            if (count == 1)
                return docs.First().Category;
            else
                return null;
        }

        public IReadOnlyCollection<IDocument>? GetSelectedDocuments()
        {
            return SelectedItems?.OfType<DatabaseExplorerDocument>().Select(d => d.Document).ToList();
        }

        public event Action<DatabaseExplorerItem>? OnSelectItem;

        public void Select(IDocument document)
        {
            if (Tree != null && ProjectObservable.Value != null)
            {
                var cat = ProjectObservable.Value.SchemafulDatabase.GetDocumentCategory(document);
                var catItem = Tree.Categories.First(c => c.Category == cat);
                catItem.IsExpanded = true;
                if (cat.Group != null && Tree.CategoryGroups.TryGetValue(cat.Group, out var group))
                {
                    group.IsExpanded = true;
                }
                DatabaseExplorerItem? selectedItem = null;
                foreach (var item in EnumerableHelper.EnumerateCompositeCollection(Tree.Items).Cast<DatabaseExplorerItem>())
                {
                    if (item is DatabaseExplorerDocument deDoc && deDoc.Document == document)
                    {
                        selectedItem = item;
                        break;
                    }
                }

                if (selectedItem != null)
                    OnSelectItem?.Invoke(selectedItem);
            }
        }

        private void SchemafulDatabaseChanged(SchemafulDatabase? dbView)
        {
            Tree = dbView == null ? null : new DatabaseExplorerTree(dbView, CollapsedCategories, Filter, optionManager.GetContextMenu<IDocument>(), optionManager.GetContextMenu<Category>());
        }

        private void UpdateFilter()
        {
            if (Tree != null)
                Tree.Filter = Filter;
            isUpdatingFilter = false;
        }

        private bool HasDatabase()
        {
            return Tree != null;
        }
    }
}