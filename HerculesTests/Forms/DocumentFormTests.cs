using Hercules.Documents;
using Hercules.Forms.Elements;
using Hercules.Forms.Schema;
using Hercules.Shortcuts;
using Json;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Hercules.Forms.Tests
{
    [TestFixture]
    public class DocumentFormTests
    {
        [Test]
        public void CategoryTest()
        {
            var rootVariantSchema = new SchemaVariant("Card", "category", null, null);
            var categoryEnum = new SchemaEnum("Category", ["test"]);
            rootVariantSchema.Fields.Add(new SchemaField("category", "category:", new EnumSchemaType(categoryEnum), 0, true));
            var testCardSchema = new SchemaRecord("CardTest", rootVariantSchema, null, "test");
            rootVariantSchema.Children.Add(testCardSchema);

            var data = new JsonObject
            {
                ["category"] = "test",
            };

            var form = CreateForm(data, data, testCardSchema);
            ClassicAssert.AreEqual(0, form.Root.SchemalessFields.Count);
            ClassicAssert.AreEqual(0, form.Root.Record.Children.Count);
            ClassicAssert.AreEqual(false, form.IsModified.Value);
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
            ClassicAssert.AreEqual(2, form.Root.SchemalessFields.Count);
            ClassicAssert.IsFalse(form.IsModified.Value);
            ClassicAssert.AreEqual(ImmutableJson.Create(1), form.Root.SchemalessFields[0].Json);
            ClassicAssert.AreEqual("custom_field1", form.Root.SchemalessFields[0].Name);
            ClassicAssert.AreEqual(ImmutableJson.Create("hello"), form.Root.SchemalessFields[1].Json);
            ClassicAssert.AreEqual("custom_field2", form.Root.SchemalessFields[1].Name);
            ClassicAssert.AreEqual(originalData.ToImmutable(), form.Json);

            form.Run(t => form.Root.RemoveSchemalessField(form.Root.SchemalessFields[0], t));
            ClassicAssert.AreEqual(1, form.Root.SchemalessFields.Count);
            ClassicAssert.IsTrue(form.IsModified.Value);
            ClassicAssert.AreEqual(new JsonObject { ["custom_field2"] = "hello" }.ToImmutable(), form.Json);
    
            form.Undo();
            ClassicAssert.AreEqual(2, form.Root.SchemalessFields.Count);
            ClassicAssert.AreEqual(originalData.ToImmutable(), form.Json);
            ClassicAssert.IsFalse(form.History.CanUndo);
            ClassicAssert.IsTrue(form.History.CanRedo);
            ClassicAssert.IsFalse(form.IsModified.Value);

            form.Redo();
            ClassicAssert.AreEqual(1, form.Root.SchemalessFields.Count);
            ClassicAssert.AreEqual(new JsonObject { ["custom_field2"] = "hello" }.ToImmutable(), form.Json);
            ClassicAssert.IsTrue(form.History.CanUndo);
            ClassicAssert.IsFalse(form.History.CanRedo);
            ClassicAssert.IsTrue(form.IsModified.Value);
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
            ClassicAssert.AreEqual(expected, form.Json);
            form.Undo();
            ClassicAssert.IsTrue(form.History.CanRedo);
            ClassicAssert.IsFalse(form.History.CanUndo);
            ClassicAssert.AreEqual(originalJson, form.Json);
            form.Redo();
            ClassicAssert.IsTrue(form.History.CanUndo);
            ClassicAssert.IsFalse(form.History.CanRedo);
            ClassicAssert.AreEqual(expected, form.Json);
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
            ClassicAssert.AreEqual(expected, form.Json);
            form.Undo();
            ClassicAssert.IsTrue(form.History.CanRedo);
            ClassicAssert.IsFalse(form.History.CanUndo);
            ClassicAssert.AreEqual(originalJson, form.Json);
            form.Redo();
            ClassicAssert.IsTrue(form.History.CanUndo);
            ClassicAssert.IsFalse(form.History.CanRedo);
            ClassicAssert.AreEqual(expected, form.Json);
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
            ClassicAssert.AreEqual(expected, form.Json);
            form.Undo();
            ClassicAssert.IsTrue(form.History.CanRedo);
            ClassicAssert.IsFalse(form.History.CanUndo);
            ClassicAssert.AreEqual(originalJson, form.Json);
            form.Redo();
            ClassicAssert.IsTrue(form.History.CanUndo);
            ClassicAssert.IsFalse(form.History.CanRedo);
            ClassicAssert.AreEqual(expected, form.Json);
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
            ClassicAssert.AreEqual(newJson, form.Json);
            ClassicAssert.IsFalse(form.IsModified.Value);
            ClassicAssert.IsTrue(form.Root.IsValid);
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
                ClassicAssert.IsTrue(form.ElementByPath(new JsonPath("bool1")).IsModified);
                ClassicAssert.IsFalse(form.ElementByPath(new JsonPath("bool2")).IsModified);
            }

            {
                var data = new JsonObject { ["bool1"] = false, ["bool2"] = false };
                var form = CreateForm(data, data, rootRecord);
                ClassicAssert.IsFalse(form.ElementByPath(new JsonPath("bool1")).IsModified);
                ClassicAssert.IsFalse(form.ElementByPath(new JsonPath("bool2")).IsModified);
            }

            {
                var data = new JsonObject { ["bool1"] = 1, ["bool2"] = 1 };
                var form = CreateForm(data, data, rootRecord);
                ClassicAssert.IsTrue(form.ElementByPath(new JsonPath("bool1")).IsModified);
                ClassicAssert.IsTrue(form.ElementByPath(new JsonPath("bool2")).IsModified);
            }

            {
                var data = new JsonObject { ["bool1"] = ImmutableJson.Null, ["bool2"] = ImmutableJson.Null };
                var form = CreateForm(data, data, rootRecord);
                ClassicAssert.IsTrue(form.ElementByPath(new JsonPath("bool1")).IsModified);
                ClassicAssert.IsTrue(form.ElementByPath(new JsonPath("bool2")).IsModified);
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
            ClassicAssert.IsFalse(element.IsSet);
            ClassicAssert.IsFalse(form.IsModified.Value);
            element.ToggleCommand.Execute(null);
            ClassicAssert.IsTrue(element.IsSet);
            ClassicAssert.AreEqual(new JsonObject { ["value"] = 1 }.ToImmutable(), form.Json);
            ClassicAssert.IsTrue(form.IsModified.Value);
            form.Undo();
            ClassicAssert.IsFalse(element.IsSet);
            ClassicAssert.AreEqual(new JsonObject { ["value"] = ImmutableJson.Null }.ToImmutable(), form.Json);
            ClassicAssert.IsTrue(form.History.CanRedo);
            ClassicAssert.IsFalse(form.History.CanUndo);
            ClassicAssert.IsFalse(form.IsModified.Value);
            form.Redo();
            ClassicAssert.IsTrue(element.IsSet);
            ClassicAssert.AreEqual(new JsonObject { ["value"] = 1 }.ToImmutable(), form.Json);
            ClassicAssert.IsFalse(form.History.CanRedo);
            ClassicAssert.IsTrue(form.IsModified.Value);
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
            ClassicAssert.IsFalse(optElement.IsSet);
            ClassicAssert.IsFalse(form.IsModified.Value);
            intElement.Value = 2;
            ClassicAssert.IsTrue(optElement.IsSet);
            ClassicAssert.AreEqual(new JsonObject { ["value"] = 2 }.ToImmutable(), form.Json);
            ClassicAssert.IsTrue(form.History.CanUndo);
            ClassicAssert.IsFalse(form.History.CanRedo);
            ClassicAssert.IsTrue(form.IsModified.Value);
            form.Undo();
            ClassicAssert.IsFalse(optElement.IsSet);
            ClassicAssert.IsFalse(intElement.IsActive);
            ClassicAssert.IsFalse(form.History.CanUndo);
            ClassicAssert.IsFalse(form.IsModified.Value);
            form.Redo();
            ClassicAssert.IsTrue(optElement.IsSet);
            ClassicAssert.AreEqual(new JsonObject { ["value"] = 2 }.ToImmutable(), form.Json);
            ClassicAssert.IsFalse(form.History.CanRedo);
            ClassicAssert.IsTrue(form.History.CanUndo);
            ClassicAssert.IsTrue(form.IsModified.Value);
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
            ClassicAssert.IsTrue(form.IsModified.Value);
            var newOriginal = new JsonObject { ["value"] = new JsonArray { 1 } }.ToImmutable();
            form.SetOriginalJson(newOriginal);
            ClassicAssert.IsFalse(form.IsModified.Value);
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
            ClassicAssert.AreEqual("1", intElement.JsonKey);
            ClassicAssert.IsFalse(form.IsModified.Value);
        }

        private DocumentForm CreateForm(ImmutableJsonObject json, ImmutableJson? originalJson, SchemaRecord schemaRecord)
        {
            var elementFactory = new ElementFactory(new CustomTypeRegistry(), new ElementFactoryContext(null));
            var formSettings = new FormSettings();
            return new DocumentForm(json, originalJson, schemaRecord, elementFactory, formSettings);
        }
    }
}
