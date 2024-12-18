using Hercules.Documents;
using Json;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace Hercules.DB.Tests
{
    [TestFixture]
    public sealed class DatabaseTests : IDisposable
    {
        private DatabaseBackendStub backend = default!;
        private TestScheduler scheduler = default!;
        private Database database = default!;
        private DatabaseCacheStub cache = default!;
        private TempStorage tempStorage = default!;

        [OneTimeSetUp]
        public void Init()
        {
            tempStorage = TempStorage.Create();
        }

        [SetUp]
        public void SetUp()
        {
            scheduler = new TestScheduler();
            backend = new DatabaseBackendStub(scheduler);
            var cacheEntries = new List<ImmutableJsonObject>
            {
                new JsonObject { ["_id"] = "spell_fireball", ["_rev"] = "1-a", ["name"] = "Fireball" },
                new JsonObject { ["_id"] = "spell_roots", ["_rev"] = "1-a", ["name"] = "Roots" },
                new JsonObject { ["_id"] = "schema", ["_rev"] = "1-a" },
            };
            cache = new DatabaseCacheStub(cacheEntries, "1");
            database = new Database(backend, cache, tempStorage);
        }

        [Test]
        public void LoadCache()
        {
            database.LoadCache(CancellationToken.None);
            Assert.That(database.Documents["spell_fireball"].IsExisting);
            Assert.That(database.Documents["schema"].IsExisting);
        }

        [Test]
        public void ListenForChanges_Update()
        {
            database.LoadCache(CancellationToken.None);
            database.ListenForChanges(scheduler);
            backend.PushUpdate(new JsonObject { ["_id"] = "spell_fireball", ["_rev"] = "2-b", ["name"] = "Fireball v2" });
            scheduler.Start();

            var document = database.Documents["spell_fireball"];
            ClassicAssert.AreEqual("Fireball v2", document.CurrentRevision!.Json["name"].AsString);
            ClassicAssert.AreEqual("2-b", document.CurrentRevision.Rev);
            AssertCacheConsistency(document);
        }

        [Test]
        public void ListenForChanges_Delete()
        {
            database.LoadCache(CancellationToken.None);
            database.ListenForChanges(scheduler);
            var document = database.Documents["spell_fireball"];
            backend.PushDelete("spell_fireball");
            scheduler.Start();

            ClassicAssert.AreEqual(false, document.IsExisting);
            ClassicAssert.AreEqual(false, database.Documents.ContainsKey("spell_fireball"));
            AssertCacheConsistency(document);
        }

        [Test]
        public void ListenForChanges_Create()
        {
            database.LoadCache(CancellationToken.None);
            database.ListenForChanges(scheduler);
            backend.PushUpdate(new JsonObject { ["_id"] = "summon_goblin", ["_rev"] = "1-c" });
            scheduler.Start();

            var document = database.Documents["summon_goblin"];
            ClassicAssert.AreEqual(true, document.IsExisting);
            ClassicAssert.AreEqual("1-c", document.CurrentRevision!.Rev);
            AssertCacheConsistency(document);
        }

        [Test]
        public void SaveDocument_ChangeBeforeSaved()
        {
            database.LoadCache(CancellationToken.None);
            database.ListenForChanges(scheduler);
            var lastSeq = int.Parse(cache.ReadLastSequence(), CultureInfo.InvariantCulture);

            var document = database.Documents["spell_roots"];

            var newJson = new JsonObject(document.CurrentRevision!.Json)
            {
                ["name"] = "Roots v2"
            };
            _ = database.SaveDocumentAsync(document, new DocumentDraft(newJson.ToImmutable()), new MetadataDraft("test"));

            CouchUtils.SetRevision(newJson, "2-b");

            backend.PushUpdate(newJson);
            backend.PushSaved("spell_roots", "2-b");

            scheduler.Start();

            ClassicAssert.AreEqual("Roots v2", document.CurrentRevision.Json["name"].AsString);
            ClassicAssert.AreEqual("2-b", document.CurrentRevision.Rev);
            AssertCacheConsistency(document);
            ClassicAssert.AreEqual((lastSeq + 1).ToString(CultureInfo.InvariantCulture), cache.ReadLastSequence());
        }

        [Test]
        public void SaveDocument_SavedBeforeChange()
        {
            database.LoadCache(CancellationToken.None);
            database.ListenForChanges(scheduler);
            var lastSeq = int.Parse(cache.ReadLastSequence(), CultureInfo.InvariantCulture);

            var document = database.Documents["spell_roots"];

            var newJson = new JsonObject(document.CurrentRevision!.Json)
            {
                ["name"] = "Roots v2"
            };
            _ = database.SaveDocumentAsync(document, new DocumentDraft(newJson.ToImmutable()), new MetadataDraft("test"));

            CouchUtils.SetRevision(newJson, "2-d404cd203d419612a74a207ccb0dbac8");

            backend.PushSaved("spell_roots", "2-d404cd203d419612a74a207ccb0dbac8");
            backend.PushUpdate(newJson);

            scheduler.Start();

            ClassicAssert.AreEqual("Roots v2", document.CurrentRevision.Json["name"].AsString);
            ClassicAssert.AreEqual("2-d404cd203d419612a74a207ccb0dbac8", document.CurrentRevision.Rev);
            AssertCacheConsistency(document);
            ClassicAssert.AreEqual((lastSeq + 1).ToString(CultureInfo.InvariantCulture), cache.ReadLastSequence());
        }

        void AssertCacheConsistency(IDocument document)
        {
            if (document.IsExisting)
            {
                ClassicAssert.AreEqual(document.CurrentRevision!.Rev, cache.ReadRevision(document.DocumentId));
                ClassicAssert.AreEqual(document.CurrentRevision.Json, cache.TryReadDocument(document.DocumentId, document.CurrentRevision.Rev));
            }
            else
            {
                ClassicAssert.AreEqual(null, cache.ReadRevision(document.DocumentId));
            }
        }

        public void Dispose()
        {
            tempStorage?.Dispose();
        }
    }
}