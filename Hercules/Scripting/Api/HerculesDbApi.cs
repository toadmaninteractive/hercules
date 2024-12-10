using Hercules.DB;
using Hercules.Scripting.JavaScript;
using Jint.Native;
using Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace Hercules.Scripting
{
    public class HerculesDbApi
    {
        private readonly DatabaseScriptContext databaseContext;
        public static readonly CompletionData[] Completion = ScriptingApiHelper.GetCompletionData(typeof(HerculesDbApi));

        public Dictionary<string, object> Api => ScriptingApiHelper.GetApi(this);

        public ScriptContext Context { get; }
        public JsHost Host { get; }

        public HerculesDbApi(ScriptContext context, DatabaseScriptContext databaseContext, JsHost host)
        {
            Context = context;
            this.databaseContext = databaseContext;
            Host = host;
        }

        [ScriptingApi("connection", "Connection",
            Example = "hercules.log(hercules.db.connection.title);")]
        public JsValue Connection => Host.JsonToJsValue(HerculesJsApi.ConnectionToJson(databaseContext.Connection));

        [ScriptingApi("get", "Get document by id.",
            Example = "hercules.db.get(\"my_document\");")]
        public JsValue Get(string documentId, string? rev = null)
        {
            if (rev == null)
                return Host.JsonToJsValue(databaseContext.GetJson(documentId));
            else
                return Host.JsonToJsValue(Context.InvokeNowAsync(() => databaseContext.History.LoadRevisionAsync(documentId, rev, default)).Json);
        }

        [ScriptingApi("update", "Update document.",
            Example = "hercules.db.update(\"doc\");")]
        public void Update(JsValue data)
        {
            var json = Host.JsValueToJson(data);
            var docId = CouchUtils.GetId(json.AsObject);
            databaseContext.UpdateJson(docId, json.AsObject);
        }

        [ScriptingApi("create", "Create document.",
            Example = "hercules.db.create(\"my_document\", json);")]
        public JsValue Create(string documentId, JsValue data)
        {
            var jsonObject = new Json.JsonObject(Host.JsValueToJson(data).AsObject);
            CouchUtils.SetId(jsonObject, documentId);
            databaseContext.CreateJson(documentId, jsonObject);
            return Get(documentId);
        }

        [ScriptingApi("getOrCreate", "Get or create document.",
            Example = "hercules.db.getOrCreate(\"my_document\", defaultJson);")]
        public JsValue GetOrCreate(string documentId, JsValue defaultData)
        {
            if (databaseContext.GetJson(documentId) == null)
                Create(documentId, defaultData);
            return Get(documentId);
        }

        [ScriptingApi("updateOrCreate", "Update or create document.",
            Example = "hercules.db.updateOrCreate(\"my_document\", json);")]
        public JsValue UpdateOrCreate(string documentId, JsValue data)
        {
            var jsonObject = new Json.JsonObject(Host.JsValueToJson(data).AsObject);
            if (databaseContext.GetJson(documentId) == null)
            {
                CouchUtils.SetId(jsonObject, documentId);
                databaseContext.CreateJson(documentId, jsonObject);
            }
            else
            {
                databaseContext.UpdateJson(documentId, jsonObject);
            }
            return Get(documentId);
        }

        [ScriptingApi("save", "Save document.",
            Example = "hercules.db.save(\"my_document\", json);")]
        public void Save(string documentId, JsValue? data)
        {
            var jsonObject = data == null ? null : Host.JsValueToJson(data).AsObject;
            databaseContext.Save(documentId, jsonObject);
        }

        [ScriptingApi("saveAll", "Save all documents.",
            Example = "hercules.db.saveAll();")]
        public void SaveAll()
        {
            databaseContext.SaveAll();
        }

        [ScriptingApi("delete", "Delete document by id.",
            Example = "hercules.db.delete(\"my_document\");")]
        public void Delete(string documentId, bool force = false)
        {
            databaseContext.Delete(documentId, !force);
        }

        [ScriptingApi("idsByCategory", "Get the list of document ids by category.",
            Example = "hercules.db.idsByCategory(\"my_category\");")]
        public JsValue IdsByCategory(string category)
        {
            var cat = databaseContext.SchemafulDatabase.GetCategory(category);
            if (cat != null)
                return Host.JsonToJsValue(new Json.JsonArray(cat.Documents.Select(doc => Json.ImmutableJson.Create(doc.DocumentId))));
            else
                return JsValue.Undefined;
        }

        [ScriptingApi("docsByCategory", "Get the list of documents by category.",
            Example = "hercules.db.docsByCategory(\"my_category\");")]
        public JsValue DocsByCategory(string category)
        {
            var cat = databaseContext.SchemafulDatabase.GetCategory(category);
            if (cat != null)
            {
                var docs = new Json.JsonArray(cat.Documents.Select(doc => doc.Json));
                return Host.JsonToJsValue(docs);
            }
            else
                return JsValue.Undefined;
        }

        [ScriptingApi("getAllDocs", "Get the list of all documents.",
            Example = "hercules.db.getAllDocs(includeSchemaless);")]
        public JsValue GetAllDocs(bool includeSchemaless = false)
        {
            var docs = databaseContext.SchemafulDatabase.SchemafulDocuments.Select(doc => doc.Json);
            if (includeSchemaless)
                docs = docs.Concat(databaseContext.SchemafulDatabase.SchemalessDocuments.Documents.Select(doc => doc.Json));
            return Host.JsonToJsValue(new Json.JsonArray(docs));
        }

        [ScriptingApi("getDocRevisions", "Get the list of document revisions.")]
        public JsValue GetDocRevisions(string docId)
        {
            var commits = Context.InvokeNowAsync<IReadOnlyList<DocumentCommit>>(() => databaseContext.History.GetDocumentRevisionsAsync(docId, default));
            return Host.JsonToJsValue(new JsonArray(commits.Select(c => ImmutableJson.Create(c.Revision))));
        }

        [ScriptingApi("getHistory", "Get the list of document revisions.")]
        public object[] GetHistory(DateTime since)
        {
            object CommitToJson(DocumentCommit commit)
            {
                return new { id = commit.DocumentId, rev = commit.Revision, time = commit.Time!.Value, user = commit.User, prevRev = commit.PreviousRevision };
            }

            var commits = Context.InvokeNowAsync(() => databaseContext.History.GetHistoryAsync(since, default));
            return commits.Select(CommitToJson).ToArray();
        }
    }
}
