using Hercules.Connections;
using Hercules.DB;
using Hercules.Documents;
using Hercules.Search;
using Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using Hercules.Scripting.JavaScript;
using System.Threading;
using System.Windows;

namespace Hercules.Scripting
{
    public class ScriptingModuleProvider : IJsModuleProvider
    {
        private readonly Database database;

        public ScriptingModuleProvider(Database database)
        {
            this.database = database;
        }

        public string GetModuleCode(string name)
        {
            if (database.Documents.TryGetValue(name, out var document) && document.Json.TryGetValue("script", out var script) && script.IsString)
            {
                return script.AsString;
            }
            else
            {
                throw new InvalidOperationException($"Module {name} not found");
            }
        }
    }

    public class DatabaseScriptContext
    {
        public Database Database { get; }
        private DatabaseHistory? history;
        public DatabaseHistory History => history ??= new DatabaseHistory(Database);
        public DbConnection Connection { get; }
        public SchemafulDatabase SchemafulDatabase { get; }
        public Dictionary<string, ImmutableJsonObject> DocumentCache { get; } = new();
        public List<string> DeletedDocumentIds { get; } = new List<string>();
        private static readonly EqualityComparer<ImmutableJson> jsonEqualityComparer = new JsonPartialEqualityComparer(excludeKeys: new[] { "_id", "_rev", "_attachments", CouchUtils.HerculesBase, "hercules_metadata" });
        public ScriptContext ScriptContext { get; }

        public DatabaseScriptContext(ScriptContext scriptContext, Database database, DbConnection connection, SchemafulDatabase schemafulDatabase)
        {
            this.Database = database;
            this.Connection = connection;
            SchemafulDatabase = schemafulDatabase;
            ScriptContext = scriptContext;
        }

        public void Delete(string documentId, bool askConfirmation)
        {
            if (askConfirmation)
                DeletedDocumentIds.Add(documentId);
            else
            {
                if (Database.Documents.TryGetValue(documentId, out var doc))
                    ScriptContext.InvokeAsync(() => Database.DeleteDocumentAsync(doc));
            }
        }

        public Task SaveDocumentAsync(IDocument document, DocumentDraft draft)
        {
            var metadata = new MetadataDraft(Connection.Username);
            return Database.SaveDocumentAsync(document, draft, metadata);
        }

        public void Save(string documentId, ImmutableJsonObject? json)
        {
            if (Database.Documents.TryGetValue(documentId, out var document))
            {
                var draft = new DocumentDraft(json ?? GetJson(documentId)!.AsObject, document.Attachments);
                ScriptContext.InvokeAsync(() => SaveDocumentAsync(document, draft));
            }
            else
            {
                var newJson = json ?? GetJson(documentId)?.AsObject;
                if (newJson == null)
                    throw new InvalidOperationException($"Trying to save a new document '{documentId}' without a content. Please provide a content JSON.");
                var draft = new DocumentDraft(newJson);
                document = ScriptContext.InvokeNow(() => Database.CreateDocument(documentId, draft));
                ScriptContext.InvokeAsync(() => SaveDocumentAsync(document, draft));
            }
        }

        public ImmutableJson? GetJson(string documentId)
        {
            if (!DocumentCache.TryGetValue(documentId, out var json))
            {
                if (Database.Documents.TryGetValue(documentId, out var schemafulDoc))
                    return schemafulDoc.Json;
            }
            return json;
        }

        public void CreateJson(string documentId, ImmutableJsonObject json)
        {
            var oldJson = GetJson(documentId);
            if (!ImmutableJson.Equals(json, oldJson))
                DocumentCache[documentId] = json;
        }

        public void UpdateJson(string documentId, ImmutableJsonObject json)
        {
            var oldJson = GetJson(documentId);
            if (oldJson == null)
                throw new ArgumentException($"{documentId} does not exist");

            if (!jsonEqualityComparer.Equals(json, oldJson))
                DocumentCache[documentId] = json;
        }

        public void SaveAll()
        {
            foreach (var documentId in DocumentCache.Keys)
            {
                Save(documentId, null);
            }
        }

        public void Flush()
        {
            foreach (var pair in DocumentCache)
            {
                var documentId = pair.Key;
                if (!Database.Documents.ContainsKey(documentId))
                {
                    Database.CreateDocument(documentId, new DocumentDraft(pair.Value));
                }
            }

            var deletedDocuments = DeletedDocumentIds.Select(id => Database.Documents.GetValueOrDefault(id)).WhereNotNull().ToList();
            DocumentsModule.DeleteDocuments(ScriptContext.Core.Workspace, Database, deletedDocuments);
        }
    }

    public class ScriptContext
    {
        private readonly Dispatcher dispatcher;
        public Core Core { get; }
        public DatabaseScriptContext ActiveDatabaseContext { get; }
        public Dictionary<string, DatabaseScriptContext> Databases { get; } = new();
        public SearchResults SearchResults { get; } = new();
        private readonly List<Func<Task>> actions = new();
        public ScriptingModuleProvider ScriptingModuleProvider { get; }
        public List<string> LogMessages { get; } = new();

        public ScriptContext(Core core, Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.Core = core;
            ScriptingModuleProvider = new ScriptingModuleProvider(Core.Project.Database);
            ActiveDatabaseContext = new DatabaseScriptContext(this, Core.Project.Database, Core.Project.Connection, Core.Project.SchemafulDatabase);
            Databases.Add(ActiveDatabaseContext.Connection.Title, ActiveDatabaseContext);
        }

        public void Invoke(Action action)
        {
            actions.Add(() => { action(); return Task.CompletedTask; });
        }

        public void InvokeAsync(Func<Task> action)
        {
            actions.Add(action);
        }

        public T InvokeNowAsync<T>(Func<Task<T>> action)
        {
            return dispatcher.InvokeAsync(action).Result.Result;
        }

        public T InvokeNow<T>(Func<T> action)
        {
            return dispatcher.Invoke(action);
        }

        public void Log(string text)
        {
            LogMessages.Add(text);
            Logger.LogUser(text);
        }

        public void Warning(string text)
        {
            LogMessages.Add(text);
            Logger.LogWarning(text);
        }

        public void Error(string text)
        {
            LogMessages.Add(text);
            Logger.LogError(text);
        }

        public void Debug(string text)
        {
            LogMessages.Add(text);
            Logger.LogDebug(text);
        }

        public void Open(string documentId)
        {
            Invoke(() => Core.GetModule<DocumentsModule>().EditDocument(documentId));
        }

        public async Task FlushAsync()
        {
            if (Thread.CurrentThread != Application.Current.Dispatcher.Thread)
            {
                throw new InvalidOperationException("Script flush is not called from UI thread");
            }
            foreach (var action in actions)
            {
                if (Core.IsBatch)
                {
                    await action();
                }
                else
                {
                    action().Track();
                }
            }
            if (SearchResults.Documents.Count > 0)
            {
                Core.GetModule<SearchModule>().ShowSearchResults(SearchResults);
            }

            foreach (var dbContext in Databases.Values)
            {
                dbContext.Flush();
            }

            Core.GetModule<DocumentsModule>().EditDocuments(ActiveDatabaseContext.DocumentCache);
        }

        public void EmitSearchResult(string documentId, string path, string text)
        {
            SearchResults.Add(new Reference(documentId, JsonPath.Parse(path), text));
        }

        public DatabaseScriptContext LoadDatabase(string title)
        {
            if (Databases.TryGetValue(title, out var dbContext))
                return dbContext;

            var connectionsModule = Core.GetModule<ConnectionsModule>();
            var documentsModule = Core.GetModule<DocumentsModule>();
            var connection = connectionsModule.Connections.Items.FirstOrDefault(c => c.Title == title);
            if (connection == null)
                throw new InvalidOperationException($"Unknown database {title}");
            var db = dispatcher.Invoke(() => connectionsModule.OpenDatabase(connection, TempStorage.Create()));
            var newDbContext = new DatabaseScriptContext(this, db, connection, new SchemafulDatabase(db, documentsModule.FormSchemaFactory, documentsModule.MetaSchemaProvider, false));
            Databases.Add(title, newDbContext);
            return newDbContext;
        }
    }
}