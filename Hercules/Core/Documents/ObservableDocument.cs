using Hercules.Shortcuts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Linq;

namespace Hercules.Documents
{
    public interface IObservableDocument : IReadOnlyObservableValue<IDocument?>, IShortcutProvider
    {
        string DocumentId { get; set; }
    }

    public sealed class ObservableDocument : NotifyPropertyChanged, IObservableDocument, IShortcutProvider
    {
        private readonly IReadOnlyDatabase database;

        private string documentId;
        public string DocumentId
        {
            get => documentId;
            set
            {
                if (SetField(ref documentId, value))
                {
                    Value = database.Documents.GetValueOrDefault(documentId);
                    RaisePropertyChanged(nameof(Value));
                    RaisePropertyChanged(nameof(Shortcut));
                }
            }
        }

        public IDocument? Value { get; private set; }

        private readonly IDisposable databaseSubscription;

        public IDisposable Subscribe(IObserver<IDocument?> observer)
        {
            return Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(h => PropertyChanged += h, h => PropertyChanged -= h).Select(_ => Value).Subscribe(observer);
        }

        public ObservableDocument(IReadOnlyDatabase database, string documentId)
        {
            this.database = database;
            this.documentId = documentId;
            Value = database.Documents.GetValueOrDefault(documentId);
            databaseSubscription = database.Changes.Subscribe(DatabaseChanged);
        }

        public override string? ToString() => Value?.ToString();

        public void Dispose()
        {
            databaseSubscription.Dispose();
        }

        private void DatabaseChanged(DatabaseChange databaseChange)
        {
            if (databaseChange.Document.DocumentId == documentId)
            {
                switch (databaseChange.Kind)
                {
                    case DatabaseChangeKind.Add:
                    case DatabaseChangeKind.Update:
                        Value = databaseChange.Document;
                        RaisePropertyChanged(nameof(Value));
                        break;
                    case DatabaseChangeKind.Remove:
                        Value = null;
                        RaisePropertyChanged(nameof(Value));
                        break;
                }
            }
        }

        public IShortcut Shortcut => new DocumentShortcut(DocumentId);
    }
}
