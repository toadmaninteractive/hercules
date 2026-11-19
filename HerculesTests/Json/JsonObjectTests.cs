using NUnit.Framework;

namespace Json.Tests
{
    [TestFixture]
    public class JsonObjectTests
    {
        [Test]
        public void ToStringTest()
        {
            Assert.AreEqual("{}", new JsonObject().ToString());
        }

        [Test]
        public void ToImmutableTest()
        {
            var jsonObject = new JsonObject { ["a"] = true, ["b"] = 0, ["c"] = ImmutableJson.Null };
            var immutable1 = jsonObject.ToImmutable();
            jsonObject.Add("d", "test");
            var immutable2 = jsonObject.ToImmutable();
            jsonObject.Remove("d");
            var immutable3 = jsonObject.ToImmutable();
            Assert.AreNotEqual(immutable1, immutable2);
            Assert.AreEqual(immutable1, immutable3);
        }
    }
}
