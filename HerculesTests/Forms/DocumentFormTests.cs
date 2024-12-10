using Hercules.Documents;
using Hercules.Forms.Elements;
using Hercules.Forms.Schema;
using Hercules.Shortcuts;
using Json;
using NUnit.Framework;

namespace Hercules.Forms.Tests
{
    [TestFixture]
    public class DocumentFormTests
    {
        [Test]
        public void CategoryTest()
        {
            var rootVariantSchema = new SchemaVariant("Card", "category", null, null);
            var categoryEnum = new SchemaEnum("Category", new[] { "test" });
            rootVariantSchema.Fields.Add(new SchemaField("category", "category:", new EnumSchemaType(categoryEnum), 0, true));
            var testCardSchema = new SchemaRecord("CardTest", rootVariantSchema, null, "test");
            rootVariantSchema.Children.Add(testCardSchema);

            var data = new JsonObject
            {
                ["category"] = "test",
            };

            var form = CreateForm(data, data, testCardSchema);
            Assert.AreEqual(0, form.Root.SchemalessFields.Count);
            Assert.AreEqual(0, form.Root.Record.Children.Count);
            Assert.AreEqual(false, form.IsModified.Value);
        }

        [Test]
        public void CustomFieldTest()
        {
            var rootRecord = new SchemaRecord("CardTest", null, null);

            var originalData = new JsonObject
            {
                ["custom_field2"] = "hello",
                ["custom_field1"] = 1,
            };

            var form = CreateForm(originalData, originalData, rootRecord);
            Assert.AreEqual(2, form.Root.SchemalessFields.Count);
            Assert.IsFalse(form.IsModified.Value);
            Assert.AreEqual(ImmutableJson.Create(1), form.Root.SchemalessFields[0].Json);
            Assert.AreEqual("custom_field1", form.Root.SchemalessFields[0].Name);
            Assert.AreEqual(ImmutableJson.Create("hello"), form.Root.SchemalessFields[1].Json);
            Assert.AreEqual("custom_field2", form.Root.SchemalessFields[1].Name);
            Assert.AreEqual(originalData.ToImmutable(), form.Json);

            form.Run(t => form.Root.RemoveSchemalessField(form.Root.SchemalessFields[0], t));
            Assert.AreEqual(1, form.Root.SchemalessFields.Count);
            Assert.IsTrue(form.IsModified.Value);
            Assert.AreEqual(new JsonObject { ["custom_field2"] = "hello" }.ToImmutable(), form.Json);

            form.Undo();
            Assert.AreEqual(2, form.Root.SchemalessFields.Count);
            Assert.AreEqual(originalData.ToImmutable(), form.Json);
            Assert.IsFalse(form.History.CanUndo);
            Assert.IsTrue(form.History.CanRedo);
            Assert.IsFalse(form.IsModified.Value);

            form.Redo();
            Assert.AreEqual(1, form.Root.SchemalessFields.Count);
            Assert.AreEqual(new JsonObject { ["custom_field2"] = "hello" }.ToImmutable(), form.Json);
            Assert.IsTrue(form.History.CanUndo);
            Assert.IsFalse(form.History.CanRedo);
            Assert.IsTrue(form.IsModified.Value);
        }

        [Test]
        public void ListMoveDownTest()
        {
            var rootRecord = new SchemaRecord("CardTest", null, null);
            var shortcutService = new ShortcutService();
            rootRecord.Fields.Add(new SchemaField("value", "value:", new ListSchemaType(new StringSchemaType(), shortcutService), 0, false));
            var originalJson = new JsonObject { ["value"] = new JsonArray { "one", "two", "three", "four" } }.ToImmutable();
            var form = CreateForm(originalJson, originalJson, rootRecord);
            var list = (ListElement)form.ElementByPath(new JsonPath("value"));
            list.DragStarted(list.Children[1]);
            form.Run(transaction => list.Move(list.Children[1], list.Children[2], transaction));
            var expected = new JsonObject { ["value"] = new JsonArray { "one", "three", "two", "four" } }.ToImmutable();
            Assert.AreEqual(expected, form.Json);
            form.Undo();
            Assert.IsTrue(form.History.CanRedo);
            Assert.IsFalse(form.History.CanUndo);
            Assert.AreEqual(originalJson, form.Json);
            form.Redo();
            Assert.IsTrue(form.History.CanUndo);
            Assert.IsFalse(form.History.CanRedo);
            Assert.AreEqual(expected, form.Json);
        }

        [Test]
        public void ListRemoveSeveralTest()
        {
            var rootRecord = new SchemaRecord("CardTest", null, null);
            var shortcutService = new ShortcutService();
            rootRecord.Fields.Add(new SchemaField("value", "value:", new ListSchemaType(new StringSchemaType(), shortcutService), 0, false));
            var originalJson = new JsonObject { ["value"] = new JsonArray { "one", "two", "three", "four" } }.ToImmutable();
            var form = CreateForm(originalJson, originalJson, rootRecord);
            var list = (ListElement)form.ElementByPath(new JsonPath("value"));
            list.DragStarted(list.Children[1]);
            form.Run(transaction =>
            {
                list.Remove(list.Children[1], transaction);
                list.Remove(list.Children[1], transaction);
            });
            var expected = new JsonObject { ["value"] = new JsonArray { "one", "four" } }.ToImmutable();
            Assert.AreEqual(expected, form.Json);
            form.Undo();
            Assert.IsTrue(form.History.CanRedo);
            Assert.IsFalse(form.History.CanUndo);
            Assert.AreEqual(originalJson, form.Json);
            form.Redo();
            Assert.IsTrue(form.History.CanUndo);
            Assert.IsFalse(form.History.CanRedo);
            Assert.AreEqual(expected, form.Json);
        }

        [Test]
        public void ListMoveUpTest()
        {
            var rootRecord = new SchemaRecord("CardTest", null, null);
            var shortcutService = new ShortcutService();
            rootRecord.Fields.Add(new SchemaField("value", "value:", new ListSchemaType(new StringSchemaType(), shortcutService), 0, false));
            var originalJson = new JsonObject { ["value"] = new JsonArray { "one", "two", "three", "four" } }.ToImmutable();
            var form = CreateForm(originalJson, originalJson, rootRecord);
            var list = (ListElement)form.ElementByPath(new JsonPath("value"));
            list.DragStarted(list.Children[2]);
            form.Run(transaction => list.Move(list.Children[2], list.Children[1], transaction));
            var expected = new JsonObject { ["value"] = new JsonArray { "one", "three", "two", "four" } }.ToImmutable();
            Assert.AreEqual(expected, form.Json);
            form.Undo();
            Assert.IsTrue(form.History.CanRedo);
            Assert.IsFalse(form.History.CanUndo);
            Assert.AreEqual(originalJson, form.Json);
            form.Redo();
            Assert.IsTrue(form.History.CanUndo);
            Assert.IsFalse(form.History.CanRedo);
            Assert.AreEqual(expected, form.Json);
        }

        [Test]
        public void ListLastTakeRemoteTest()
        {
            var rootRecord = new SchemaRecord("CardTest", null, null);
            var shortcutService = new ShortcutService();
            rootRecord.Fields.Add(new SchemaField("value", "value:", new ListSchemaType(new StringSchemaType(), shortcutService), 0, false));
            var originalJson = new JsonObject { ["value"] = new JsonArray { "one", "two", "three" } }.ToImmutable();
            var form = CreateForm(originalJson, originalJson, rootRecord);
            var newJson = new JsonObject { ["value"] = new JsonArray { "one", "two", "three", "four" } }.ToImmutable();
            form.SetOriginalJson(newJson);
            form.Run(transaction => form.Root.SetJson(newJson, transaction));
            Assert.AreEqual(newJson, form.Json);
            Assert.IsFalse(form.IsModified.Value);
            Assert.IsTrue(form.Root.IsValid);
        }

        [Test]
        public void DefaultValueModifiedTest()
        {
            var rootRecord = new SchemaRecord("CardTest", null, null);
            rootRecord.Fields.Add(new SchemaField("bool1", "bool1:", new BoolSchemaType(), 0, false));
            rootRecord.Fields.Add(new SchemaField("bool2", "bool2:", new BoolSchemaType(@default: false), 0, false));

            {
                var data = new JsonObject();
                var form = CreateForm(data, data, rootRecord);
                Assert.IsTrue(form.ElementByPath(new JsonPath("bool1")).IsModified);
                Assert.IsFalse(form.ElementByPath(new JsonPath("bool2")).IsModified);
            }

            {
                var data = new JsonObject { ["bool1"] = false, ["bool2"] = false };
                var form = CreateForm(data, data, rootRecord);
                Assert.IsFalse(form.ElementByPath(new JsonPath("bool1")).IsModified);
                Assert.IsFalse(form.ElementByPath(new JsonPath("bool2")).IsModified);
            }

            {
                var data = new JsonObject { ["bool1"] = 1, ["bool2"] = 1 };
                var form = CreateForm(data, data, rootRecord);
                Assert.IsTrue(form.ElementByPath(new JsonPath("bool1")).IsModified);
                Assert.IsTrue(form.ElementByPath(new JsonPath("bool2")).IsModified);
            }

            {
                var data = new JsonObject { ["bool1"] = ImmutableJson.Null, ["bool2"] = ImmutableJson.Null };
                var form = CreateForm(data, data, rootRecord);
                Assert.IsTrue(form.ElementByPath(new JsonPath("bool1")).IsModified);
                Assert.IsTrue(form.ElementByPath(new JsonPath("bool2")).IsModified);
            }
        }

        [Test]
        public void OptionalFieldTest()
        {
            var rootRecord = new SchemaRecord("CardTest", null, null);
            rootRecord.Fields.Add(new SchemaField("value", "value:", new IntSchemaType(optional: true, @default: 1), 0, false));
            var data = new JsonObject();
            var form = CreateForm(data, data, rootRecord);
            var element = (OptionalElement)form.ElementByPath(new JsonPath("value"))!;
            Assert.IsFalse(element.IsSet);
            Assert.IsFalse(form.IsModified.Value);
            element.ToggleCommand.Execute(null);
            Assert.IsTrue(element.IsSet);
            Assert.AreEqual(new JsonObject { ["value"] = 1 }.ToImmutable(), form.Json);
            Assert.IsTrue(form.IsModified.Value);
            form.Undo();
            Assert.IsFalse(element.IsSet);
            Assert.AreEqual(new JsonObject { ["value"] = ImmutableJson.Null }.ToImmutable(), form.Json);
            Assert.IsTrue(form.History.CanRedo);
            Assert.IsFalse(form.History.CanUndo);
            Assert.IsFalse(form.IsModified.Value);
            form.Redo();
            Assert.IsTrue(element.IsSet);
            Assert.AreEqual(new JsonObject { ["value"] = 1 }.ToImmutable(), form.Json);
            Assert.IsFalse(form.History.CanRedo);
            Assert.IsTrue(form.IsModified.Value);
        }

        [Test]
        public void OptionalFieldAutoSetTest()
        {
            var rootRecord = new SchemaRecord("CardTest", null, null);
            rootRecord.Fields.Add(new SchemaField("value", "value:", new IntSchemaType(optional: true, @default: 1), 0, false));
            var data = new JsonObject();
            var form = CreateForm(data, data, rootRecord);
            var optElement = (OptionalElement)form.ElementByPath(new JsonPath("value"))!;
            var intElement = (IntElement)optElement.Element!;
            Assert.IsFalse(optElement.IsSet);
            Assert.IsFalse(form.IsModified.Value);
            intElement.Value = 2;
            Assert.IsTrue(optElement.IsSet);
            Assert.AreEqual(new JsonObject { ["value"] = 2 }.ToImmutable(), form.Json);
            Assert.IsTrue(form.History.CanUndo);
            Assert.IsFalse(form.History.CanRedo);
            Assert.IsTrue(form.IsModified.Value);
            form.Undo();
            Assert.IsFalse(optElement.IsSet);
            Assert.IsFalse(intElement.IsActive);
            Assert.IsFalse(form.History.CanUndo);
            Assert.IsFalse(form.IsModified.Value);
            form.Redo();
            Assert.IsTrue(optElement.IsSet);
            Assert.AreEqual(new JsonObject { ["value"] = 2 }.ToImmutable(), form.Json);
            Assert.IsFalse(form.History.CanRedo);
            Assert.IsTrue(form.History.CanUndo);
            Assert.IsTrue(form.IsModified.Value);
        }

        [Test]
        public void ListSetOriginalTest()
        {
            var rootRecord = new SchemaRecord("CardTest", null, null);
            var shortcutService = new ShortcutService();
            rootRecord.Fields.Add(new SchemaField("value", "value:", new ListSchemaType(new IntSchemaType(), shortcutService), 0, false));
            var data = new JsonObject();
            var form = CreateForm(data, data, rootRecord);
            var list = (ListElement)form.ElementByPath(new JsonPath("value"));
            form.Run(t => list.SetJson(new JsonArray { 1 }.ToImmutable(), t));
            Assert.IsTrue(form.IsModified.Value);
            var newOriginal = new JsonObject { ["value"] = new JsonArray { 1 } }.ToImmutable();
            form.SetOriginalJson(newOriginal);
            Assert.IsFalse(form.IsModified.Value);
        }

        [Test]
        public void IntegerDictKeyTest()
        {
            var rootRecord = new SchemaRecord("CardTest", null, null);
            var keySchemaType = new IntSchemaType();
            var valueSchemaType = new IntSchemaType();
            rootRecord.Fields.Add(new SchemaField("value", "value:", new DictSchemaType(keySchemaType, valueSchemaType), 0, false));
            var data = new JsonObject { ["value"] = new JsonObject { ["1"] = 10, ["2"] = 20 } };
            var form = CreateForm(data, data, rootRecord);
            var dictElement = (DictElement)form.ElementByPath(new JsonPath("value"))!;
            var intElement = (IntElement)dictElement.Children[0].KeyElement;
            Assert.AreEqual("1", intElement.JsonKey);
            Assert.IsFalse(form.IsModified.Value);
        }

        private DocumentForm CreateForm(ImmutableJsonObject json, ImmutableJson? originalJson, SchemaRecord schemaRecord)
        {
            var elementFactory = new ElementFactory(new CustomTypeRegistry(), new ElementFactoryContext(null));
            var formSettings = new FormSettings();
            return new DocumentForm(json, originalJson, schemaRecord, elementFactory, formSettings);
        }
    }
}
