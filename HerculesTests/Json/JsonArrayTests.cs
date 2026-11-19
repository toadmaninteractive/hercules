using NUnit.Framework;

namespace Json.Tests
{
    [TestFixture]
    public class JsonArrayTests
    {
        [Test]
        public void ToStringTest()
        {
            Assert.AreEqual("[]", new JsonArray().ToString());
            Assert.AreEqual("[1,2,3]", new JsonArray { 1, 2, 3 }.ToString());
        }

        [Test]
        public void ToImmutableTest()
        {
            var jsonArray = new JsonArray { 1, false, ImmutableJson.Null };
            var immutable1 = jsonArray.ToImmutable();
            jsonArray.Add("extra");
            var immutable2 = jsonArray.ToImmutable();
            jsonArray.Remove("extra");
            var immutable3 = jsonArray.ToImmutable();
            Assert.AreNotEqual(immutable1, immutable2);
            Assert.AreEqual(immutable1, immutable3);
        }
    }
}
