using NUnit.Framework;

namespace Json.Tests
{
    [TestFixture]
    public class JsonBuilderTests
    {
        public void AreEqual(ImmutableJson expected, ImmutableJson actual)
        {
            Assert.True(ImmutableJson.Equals(expected, actual));
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void EnsureTest()
        {
            ImmutableJson json = new JsonObject { ["key1"] = new JsonObject { ["key2"] = new JsonArray { true } } };

            var data = JsonBuilder.Null;
            AreEqual(ImmutableJson.Null, data.ToImmutable());

            data.GetOrAdd("key1").GetOrAdd("key2").GetOrCreateArray().Add(new JsonBuilder(true));
            AreEqual(json, data.ToImmutable());

            AreEqual(true, data.Get("key1").Get("key2").Get(0).ToImmutable());

            data.GetOrAdd("key").Set("value");
            AreEqual("value", data.GetOrAdd("key").ToImmutable());

            data.AsObject.Remove("key");
            AreEqual(json, data.ToImmutable());
        }

        [Test]
        public void OperatorTest()
        {
            ImmutableJson json = new JsonObject { ["key1"] = new JsonObject { ["key2"] = new JsonArray { true } } };

            var data3 = JsonBuilder.EmptyArray;
            AreEqual(ImmutableJson.EmptyArray, data3.ToImmutable());
            data3.AsArray.Add(new JsonBuilder(true));

            var data2 = JsonBuilder.EmptyObject;
            AreEqual(ImmutableJson.EmptyObject, data2.ToImmutable());
            data2["key2"] = data3;

            var data = JsonBuilder.Null;
            AreEqual(ImmutableJson.Null, data.ToImmutable());
            data["key1"] = data2;

            AreEqual(json, data.ToImmutable());

            AreEqual(true, data["key1"]["key2"][0].ToImmutable());

            data["key"] = new JsonBuilder("value");
            AreEqual("value", data["key"].ToImmutable());

            data.AsObject.Remove("key");
            AreEqual(json, data.ToImmutable());
        }
    }
}
