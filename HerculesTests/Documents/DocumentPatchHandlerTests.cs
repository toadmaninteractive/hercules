using Hercules.Forms.Schema;
using Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;

namespace Hercules.Documents.Editor.Tests
{
    [TestFixture]
    public class DocumentPatchHandlerTests
    {
        [SetUp]
        public void SetUp()
        {
            // implicitly register pack & application URI schemes
            var current = Application.Current;
        }

        [Test]
        public void RemoteDocumentLinkTest()
        {
            var docs = new List<ImmutableJsonObject>
            {
                new JsonObject { ["_id"] = "spell_fireball", ["_rev"] = "1-a", ["name"] = "Fireball", ["category"] = "spell" },
                new JsonObject { ["_id"] = "spell_roots", ["_rev"] = "1-a", ["name"] = "Roots", ["category"] = "spell" },
                new JsonObject { ["_id"] = "schema", ["_rev"] = "1-a" },
            };
            var db = docs.Select(json => new TestDocument(json)).ToDictionary(doc => doc.DocumentId, doc => doc);
            var editorContext = TestDocumentEditorContext.Create(db, Observable.Never<DatabaseChange>(), GetTestFormSchema());
            var fireball = db["spell_fireball"];
            var editor = new DocumentEditorPage(editorContext, fireball);
            fireball.RemoteUpdated(new JsonObject(fireball.Json) { [CouchUtils.HerculesBase] = db["spell_roots"].Json.WithoutKeys(CouchUtils.BaseJsonExcludedKeys) });
            Assert.IsTrue(editor.Notifications.Items.OfType<DocumentChangedNotification>().Any());
            editor.Notifications.Items.OfType<DocumentChangedNotification>().First().TakeRemoteCommand.Execute(null);
            Assert.IsTrue(editor.PatchHandler.IsPatch);
            Assert.AreEqual("spell_roots", editor.PatchHandler.BaseDocument!.DocumentId);
            Assert.IsFalse(editor.IsDirty);
        }

        [Test]
        public void RemoteDocumentUnlinkTest()
        {
            var docs = new List<ImmutableJsonObject>
            {
                new JsonObject { ["_id"] = "spell_fireball", ["_rev"] = "1-a", ["name"] = "Fireball", ["category"] = "spell", [CouchUtils.HerculesBase] = new JsonObject { ["_id"] = "spell_roots", ["name"] = "Roots", ["category"] = "spell" } },
                new JsonObject { ["_id"] = "spell_roots", ["_rev"] = "1-a", ["name"] = "Roots", ["category"] = "spell" },
                new JsonObject { ["_id"] = "schema", ["_rev"] = "1-a" },
            };
            var db = docs.Select(json => new TestDocument(json)).ToDictionary(doc => doc.DocumentId, doc => doc);
            var editorContext = TestDocumentEditorContext.Create(db, Observable.Never<DatabaseChange>(), GetTestFormSchema());
            var fireball = db["spell_fireball"];
            var editor = new DocumentEditorPage(editorContext, fireball);
            fireball.RemoteUpdated(new JsonObject(fireball.Json).WithoutKeys(CouchUtils.HerculesBase));
            Assert.IsTrue(editor.Notifications.Items.OfType<DocumentChangedNotification>().Any());
            editor.Notifications.Items.OfType<DocumentChangedNotification>().First().TakeRemoteCommand.Execute(null);
            Assert.IsFalse(editor.PatchHandler.IsPatch);
            Assert.IsFalse(editor.IsDirty);
        }

        [Test]
        public void RemoteDocumentLinkTakeMineTest()
        {
            var docs = new List<ImmutableJsonObject>
            {
                new JsonObject { ["_id"] = "spell_fireball", ["_rev"] = "1-a", ["name"] = "Fireball", ["category"] = "spell" },
                new JsonObject { ["_id"] = "spell_roots", ["_rev"] = "1-a", ["name"] = "Roots", ["category"] = "spell" },
                new JsonObject { ["_id"] = "schema", ["_rev"] = "1-a" },
            };
            var db = docs.Select(json => new TestDocument(json)).ToDictionary(doc => doc.DocumentId, doc => doc);
            var editorContext = TestDocumentEditorContext.Create(db, Observable.Never<DatabaseChange>(), GetTestFormSchema());
            var fireball = db["spell_fireball"];
            var editor = new DocumentEditorPage(editorContext, fireball);
            fireball.RemoteUpdated(new JsonObject(fireball.Json) { [CouchUtils.HerculesBase] = db["spell_roots"].Json.WithoutKeys(CouchUtils.BaseJsonExcludedKeys) });
            Assert.IsTrue(editor.Notifications.Items.OfType<DocumentChangedNotification>().Any());
            editor.Notifications.Items.OfType<DocumentChangedNotification>().First().TakeMineCommand.Execute(null);
            Assert.IsFalse(editor.PatchHandler.IsPatch);
            Assert.IsTrue(editor.IsDirty);
        }

        [Test]
        public void RemoteDocumentUnlinkTakeMineTest()
        {
            var docs = new List<ImmutableJsonObject>
            {
                new JsonObject { ["_id"] = "spell_fireball", ["_rev"] = "1-a", ["name"] = "Fireball", ["category"] = "spell", [CouchUtils.HerculesBase] = new JsonObject { ["_id"] = "spell_roots", ["name"] = "Roots", ["category"] = "spell" } },
                new JsonObject { ["_id"] = "spell_roots", ["_rev"] = "1-a", ["name"] = "Roots", ["category"] = "spell" },
                new JsonObject { ["_id"] = "schema", ["_rev"] = "1-a" },
            };
            var db = docs.Select(json => new TestDocument(json)).ToDictionary(doc => doc.DocumentId, doc => doc);
            var editorContext = TestDocumentEditorContext.Create(db, Observable.Never<DatabaseChange>(), GetTestFormSchema());
            var fireball = db["spell_fireball"];
            var editor = new DocumentEditorPage(editorContext, fireball);
            fireball.RemoteUpdated(new JsonObject(fireball.Json).WithoutKeys(CouchUtils.HerculesBase));
            Assert.IsTrue(editor.Notifications.Items.OfType<DocumentChangedNotification>().Any());
            editor.Notifications.Items.OfType<DocumentChangedNotification>().First().TakeMineCommand.Execute(null);
            Assert.IsTrue(editor.PatchHandler.IsPatch);
            Assert.IsTrue(editor.IsDirty);
            Assert.AreEqual("spell_roots", editor.PatchHandler.BaseDocument!.DocumentId);
        }

        FormSchema GetTestFormSchema()
        {
            var rootVariantSchema = new SchemaVariant("Card", "category", null, null);
            var categoryEnum = new SchemaEnum("Category", new[] { "spell" });
            rootVariantSchema.Fields.Add(new SchemaField("category", "category:", new EnumSchemaType(categoryEnum), 0, true));
            var testCardSchema = new SchemaRecord("CardSpell", rootVariantSchema, null, "spell");
            testCardSchema.Fields.Add(new SchemaField("name", "name:", new StringSchemaType(), 0, false));
            rootVariantSchema.Children.Add(testCardSchema);

            return new FormSchema(
                new Dictionary<string, SchemaEnum> { [categoryEnum.Name] = categoryEnum },
                new Dictionary<string, SchemaStruct> { [testCardSchema.Name] = testCardSchema, [rootVariantSchema.Name] = rootVariantSchema },
                rootVariantSchema,
                new Version(1, 0));
        }
    }
}
