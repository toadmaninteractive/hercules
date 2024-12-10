using System;
using System.Collections.Generic;

namespace Hercules.Documents
{
    public enum DatabaseChangeKind
    {
        Add,
        Remove,
        Update,
    }

    public class DatabaseChange
    {
        public DatabaseChangeKind Kind { get; }
        public IDocument Document { get; }

        public DatabaseChange(DatabaseChangeKind kind, IDocument document)
        {
            this.Kind = kind;
            this.Document = document;
        }
    }

    public interface IReadOnlyDatabase
    {
        IReadOnlyDictionary<string, IDocument> Documents { get; }
        IObservable<DatabaseChange> Changes { get; }
        public IObservableDocument ObserveDocument(string documentId) => new ObservableDocument(this, documentId);
    }
}
