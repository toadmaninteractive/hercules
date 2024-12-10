using Hercules.Documents;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Hercules.DatabaseExplorer
{
    public abstract class DatabaseExplorerDocumentGroup : DatabaseExplorerItem
    {
        public DatabaseExplorerTree Explorer { get; }
        public ObservableCollection<DatabaseExplorerItem> Items { get; } = new();
        public List<DatabaseExplorerDocument> Documents { get; }
        public ObservableCollection<IDocument> Source { get; }
        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                if (isExpanded != value)
                {
                    isExpanded = value;
                    RaisePropertyChanged();
                    OnExpandedChanged();
                    Invalidate();
                }
            }
        }

        public bool IsWithoutGroup { get; set; } = true;
        public bool InExpandGroup { get; set; }
        private bool isExpanded;

        protected DatabaseExplorerDocumentGroup(ObservableCollection<IDocument> source, DatabaseExplorerTree explorer, bool isInitiallyExpanded)
        {
            Explorer = explorer;
            Source = source;
            Documents = new List<DatabaseExplorerDocument>(Source.Select(doc => new DatabaseExplorerDocument(doc, explorer.DocumentMenu)));
            isExpanded = isInitiallyExpanded;

            Invalidate();
            Source.CollectionChanged += Documents_CollectionChanged;
        }

        public void ToggleExpand()
        {
            IsExpanded = !IsExpanded;
        }

        public void Invalidate()
        {
            if (IsWithoutGroup)
            {
                if (!Items.Contains(this)) Items.Insert(0, this);
            }
            else
                Items.Remove(this);

            var filter = Explorer.Filter;
            bool showCategory = string.IsNullOrEmpty(filter);
            if ((IsWithoutGroup && IsExpanded) || (!IsWithoutGroup && InExpandGroup))
            {
                int index = Items.Count == 0 ? 0 : 1;
                if (!IsWithoutGroup)
                    index = 0;

                foreach (var docView in Documents)
                {
                    bool visible = docView.UpdateFilter(filter);
                    showCategory |= visible;
                    var currentDocView = Items.Count > index ? (DatabaseExplorerDocument)Items[index] : null;
                    if (currentDocView == docView)
                    {
                        if (visible)
                            index++;
                        else
                            Items.RemoveAt(index);
                    }
                    else
                    {
                        if (visible)
                        {
                            Items.Insert(index, docView);
                            index++;
                        }
                    }
                }
                if (!showCategory)
                    Items.Clear();
                else if (IsWithoutGroup && (Items.Count == 0 || Items[0] != this))
                    Items.Insert(0, this);
            }
            else
            {
                if (IsWithoutGroup)
                    foreach (var docView in Documents)
                        showCategory |= docView.UpdateFilter(filter);
                else
                    showCategory = false;

                if (showCategory)
                {
                    if (Items.Count == 0)
                        Items.Add(this);
                    else
                    {
                        Items.Clear();
                        Items.Add(this);
                    }
                }
                else
                    Items.Clear();
            }
        }

        protected virtual void OnExpandedChanged()
        {
        }

        private void Documents_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Documents.InsertRange(e.NewStartingIndex, e.NewItems!.Cast<IDocument>().Select(doc => new DatabaseExplorerDocument(doc, Explorer.DocumentMenu)));
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems!.Cast<IDocument>())
                    {
                        Documents.RemoveAll(docItem => docItem.Document == item);
                        Items.RemoveAll(docItem => docItem is DatabaseExplorerDocument && ((DatabaseExplorerDocument)docItem).Document == item);
                    }
                    break;
                default:
                    Documents.AddRange(Source.Select(doc => new DatabaseExplorerDocument(doc, Explorer.DocumentMenu)));
                    Items.Clear();
                    break;
            }
            Invalidate();
        }
    }
}
