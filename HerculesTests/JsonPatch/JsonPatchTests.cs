using Json;
using NUnit.Framework;
using System.Text;

namespace JsonDiff.Tests
{
    [TestFixture]
    public class JsonPatchTests
    {
        JsonPatch GetDeleteArrayItemsPatch()
        {
            var patch = new JsonPatch();
            var path = JsonPath.Empty;
            patch.Chunks.Add(new JsonPatchChunk(path, null, 1));
            patch.Chunks.Add(new JsonPatchChunk(path, null, 1));
            patch.Chunks.Add(new JsonPatchChunk(path, null, 3));
            return patch;
        }

        [Test]
        public void ToJavaScript_DeleteArrayItemsTest()
        {
            var patch = GetDeleteArrayItemsPatch();
            var js = patch.ToJavaScript(JsonPath.Parse("doc"));
            var sb = new StringBuilder();
            sb.AppendLine("doc.splice(1, 2);");
            sb.AppendLine("doc.splice(3, 1);");
            Assert.AreEqual(sb.ToString(), js);
        }

        [Test]
        public void Apply_DeleteArrayItemsTest()
        {
            var patch = GetDeleteArrayItemsPatch();
            var json = JsonParser.Parse(@"[0, 1, 2, 3, 4, 5, 6]");
            var expectedJson = JsonParser.Parse(@"[0, 3, 4, 6]");
            var changedJson = patch.Apply(json);
            Assert.AreEqual(expectedJson, changedJson);
        }

        [Test]
        public void Apply_PatchObjectKeyTest()
        {
            var json = JsonParser.Parse(@"{""dict"": { ""first"": 1, ""secundo"": 2, ""third"": 3 }}");
            var expectedJson = JsonParser.Parse(@"{""dict"": { ""first"": 1, ""second"": 2, ""third"": 3 }}");
            var patch = new JsonPatch();
            var path = new JsonPath().AppendObject("dict").AppendObjectKey("secundo");
            patch.Chunks.Add(new JsonPatchChunk(path, "second"));
            var changedJson = patch.Apply(json.AsObject);
            Assert.AreEqual(expectedJson, changedJson);
        }
    }
}
