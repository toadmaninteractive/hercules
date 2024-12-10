using Hercules.Shortcuts;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Hercules.Documents
{
    public enum CategoryType
    {
        Schemaful,
        Schemaless,
        Schema,
        Script,
        Design,
    }

    public sealed class Category : IShortcutProvider
    {
        public string Name { get; }

        public string? Group { get; }

        public CategoryType Type { get; }

        public bool IsSchemaful => Type == CategoryType.Schemaful;

        private List<CategoryInterface>? interfaces;
        public IReadOnlyList<CategoryInterface> Interfaces => interfaces ??= new List<CategoryInterface>();

        public ObservableCollection<IDocument> Documents { get; } = new ObservableCollection<IDocument>();

        public Category(CategoryType type, string name, string? group)
        {
            Type = type;
            Name = name;
            Group = group;
        }

        public void AddDocument(IDocument document, bool insertSorted)
        {
            if (insertSorted)
                Documents.InsertSorted(document);
            else
                Documents.Add(document);
            if (interfaces != null)
            {
                foreach (var intf in interfaces)
                {
                    intf.AddDocument(document);
                }
            }
        }

        public void RemoveDocument(IDocument document)
        {
            Documents.Remove(document);
            if (interfaces != null)
            {
                foreach (var intf in interfaces)
                {
                    intf.RemoveDocument(document);
                }
            }
        }

        public void AddInterface(CategoryInterface intf)
        {
            interfaces ??= new List<CategoryInterface>();
            interfaces.Add(intf);
        }

        public IShortcut Shortcut => new CategoryShortcut(Name);
    }

    public sealed class CategoryInterface
    {
        public string Name { get; }
        public ObservableCollection<IDocument> Documents { get; } = new ObservableCollection<IDocument>();
        public IReadOnlyList<Category> Categories { get; }

        public void AddDocument(IDocument document)
        {
            Documents.InsertSorted(document);
        }

        public void RemoveDocument(IDocument document)
        {
            Documents.Remove(document);
        }

        public CategoryInterface(string name, IReadOnlyList<Category> categories)
        {
            Name = name;
            Categories = categories;

            foreach (var category in categories)
            {
                Documents.AddRange(category.Documents);

            }
        }
    }
}
