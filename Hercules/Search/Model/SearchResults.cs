using Json;
using System.Collections.Generic;
using System.Linq;

namespace Hercules.Search
{
    public class Reference : NotifyPropertyChanged
    {
        public JsonPath Path { get; }
        public string Text { get; }
        public string DocumentId { get; }

        public string Entry => Path.ToString();

        public bool IsExpanded { get; set; }

        bool isChecked;

        public bool IsChecked
        {
            get => isChecked;
            set => SetField(ref isChecked, value);
        }

        public Reference(string documentId, JsonPath path, string text)
        {
            DocumentId = documentId;
            Path = path;
            Text = text;
        }
    }

    public class DocumentReferences : NotifyPropertyChanged
    {
        public string DocumentId { get; private set; }
        public List<Reference> References { get; private set; }

        public string Entry => DocumentId;

        public bool IsExpanded { get; set; }

        public DocumentReferences(string documentId)
        {
            this.DocumentId = documentId;
            this.References = new List<Reference>();
            this.IsExpanded = true;
        }
    }

    public class SearchResults : NotifyPropertyChanged, Controls.Tree.ITreeModel
    {
        public Dictionary<string, DocumentReferences> Documents { get; private set; }

        public SearchResults()
        {
            this.Documents = new Dictionary<string, DocumentReferences>();
        }

        public void Add(Reference reference)
        {
            var document = AddDocument(reference.DocumentId);
            document.References.Add(reference);
        }

        public DocumentReferences AddDocument(string documentId)
        {
            if (!Documents.TryGetValue(documentId, out var document))
            {
                document = new DocumentReferences(documentId);
                Documents.Add(documentId, document);
            }
            return document;
        }

        public System.Collections.IEnumerable GetChildren(object? parent)
        {
            if (parent == null)
                return Documents.Values;
            return ((DocumentReferences)parent).References;
        }

        public bool HasChildren(object? parent)
        {
            return parent is DocumentReferences;
        }

        public IEnumerable<Reference> AllReferences => Documents.Values.SelectMany(d => d.References);
    }
}
