using Hercules.Connections;
using Hercules.DB;
using Hercules.Documents;
using Hercules.Forms.Schema;
using Hercules.Repository;
using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Hercules
{
    public class Project : NotifyPropertyChanged
    {
        public Database Database { get; }
        public DbConnection Connection { get; }
        public ProjectSettings Settings { get; }

        private SchemaUpdater SchemaUpdater { get; }
        private IFormSchemaFactory FormSchemaFactory { get; }

        private readonly ObservableValue<SchemafulDatabase> observableSchemafulDatabase;

        public IReadOnlyObservableValue<SchemafulDatabase> ObservableSchemafulDatabase => observableSchemafulDatabase;

        public SchemafulDatabase SchemafulDatabase
        {
            get => observableSchemafulDatabase.Value;
            private set
            {
                if (observableSchemafulDatabase.Value != value)
                {
                    observableSchemafulDatabase.Value = value;
                    RaisePropertyChanged();
                }
            }
        }

        readonly IDisposable schemaAvailableSubscription;
        private readonly IMetaSchemaProvider metaSchemaProvider;

        public Project(DbConnection connection, Database database, ProjectSettings settings, SchemaUpdater schemaUpdater, IFormSchemaFactory formSchemaFactory, IMetaSchemaProvider metaSchemaProvider)
        {
            SchemaUpdater = schemaUpdater;
            FormSchemaFactory = formSchemaFactory;
            Settings = settings;
            this.metaSchemaProvider = metaSchemaProvider;
            Database = database;
            Connection = connection;

            observableSchemafulDatabase = new ObservableValue<SchemafulDatabase>(new SchemafulDatabase(database, formSchemaFactory, metaSchemaProvider, true));

            database.ListenForChanges(DispatcherScheduler.Current);

            schemaAvailableSubscription = Database.Changes
                .Where(change => (change.Kind == DatabaseChangeKind.Update || change.Kind == DatabaseChangeKind.Add) && IsSchemaDocument(change.Document))
                .Subscribe(change => schemaUpdater.SchemaUpdateAvailable(ApplySchemaUpdate));
        }

        bool IsSchemaDocument(IDocument document)
        {
            return document.DocumentId == CouchUtils.SchemaDocumentId || CouchUtils.GetScope(document.Json) == "schema";
        }

        public void Close()
        {
            schemaAvailableSubscription.Dispose();
            Database.Close();
            SchemaUpdater.Close();
        }

        void ApplySchemaUpdate()
        {
            SchemafulDatabase.SyncPreview = false;
            SchemafulDatabase = new SchemafulDatabase(Database, FormSchemaFactory, metaSchemaProvider, true);
        }
    }
}
