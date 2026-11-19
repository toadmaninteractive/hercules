using NUnit.Framework;
using System;
using System.Text.Json;

namespace Json.Core.Tests
{
    [TestFixture]
    public class ImmutableJsonTests
    {
        private ImmutableJson ParseString(string str)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(str);
            var reader = new Utf8JsonReader(bytes.AsSpan());
            return Utf8Json.Parse(ref reader);
        }

        public string WrapString(string str)
        {
            return "\"" + str + "\"";
        }

        public void AreEqual(ImmutableJson expected, ImmutableJson actual)
        {
            Assert.True(ImmutableJson.Equals(expected, actual));
            Assert.AreEqual(expected, actual);
        }

        public void AreNotEqual(ImmutableJson expected, ImmutableJson actual)
        {
            Assert.False(ImmutableJson.Equals(expected, actual));
            Assert.AreNotEqual(expected, actual);
        }

        public void CheckSerialization(ImmutableJson value, string str)
        {
            Assert.AreEqual(str, value.ToString());
            Assert.AreEqual(value, ParseString(str));
        }

        [Test]
        public void SerializationSimpleTest()
        {
            CheckSerialization(ImmutableJson.Null, "null");
            CheckSerialization(true, "true");
            CheckSerialization(false, "false");
            CheckSerialization(1, "1");
            CheckSerialization(-5, "-5");
            CheckSerialization(1.2345, "1.2345");
            CheckSerialization(-1.2345, "-1.2345");
        }

        [Test]
        public void SerializationNumberTest()
        {
            CheckSerialization(4.0, "4");
            CheckSerialization(-4.0, "-4");
            CheckSerialization(1.00000000000001, "1.00000000000001");
            CheckSerialization(123456789123456789.0, "1.2345678912345678E+17");
            CheckSerialization(1527519502732, "1527519502732");
        }

        [Test]
        public void SerializationStringTest()
        {
            CheckSerialization("test", WrapString("test"));
            CheckSerialization("\"\\\n\t", WrapString(@"\""\\\n\t"));

            // \b \f \/ do not escape in ToString
            var bft = "\b\f/";
            var bftEscape = @"\b\f\/";
            Assert.AreEqual(bft, ParseString(WrapString(bftEscape)).AsString);
            Assert.AreEqual(WrapString(bft), ImmutableJson.Create(bft).ToString());

            // skip \r
            Assert.AreEqual(WrapString("hello"), ImmutableJson.Create("h\re\rl\rl\ro").ToString());
        }

        [Test]
        public void SerializationComplexTest()
        {
            CheckSerialization(new JsonArray { "1", 2, -3.1 }, @"[""1"",2,-3.1]");
            CheckSerialization(new JsonObject { ["key"] = true }, @"{""key"":true}");
            CheckSerialization(new JsonObject { ["key1"] = 1, ["key2"] = 2 }, @"{""key1"":1,""key2"":2}");
        }

        [Test]
        public void EqualNumbersTest()
        {
            AreEqual(0.0, 0);
            AreEqual(1.0, 1);
            AreNotEqual(1.1, 1);
            AreEqual(1.00000000000001, 1);
            AreNotEqual(1.00000000000002, 1);
            AreEqual(-1.00000000000001, -1);
            AreEqual(12345678912345.0, 12345678912345.1);
            AreNotEqual(12345678912345.0, 12345678912345.2);
            AreEqual(1527519502732, 1527519502732);
        }

        [Test]
        public void EqualObjectTest()
        {
            ImmutableJson json = new JsonObject
            {
                ["key1"] = 1,
                ["key2"] = -2,
                ["key3"] = ImmutableJson.Null,
                ["key4"] = true,
                ["key5"] = false,
                ["key6"] = 1.0,
                ["arr"] = new JsonArray
                    {
                        "value", 1, 1.0, true, ImmutableJson.Null,
                        new JsonObject
                        {
                            ["a"] = 10,
                            ["b"] = -10
                        },
                        new JsonArray { 1, 2, 3, 4, 5 }
                    },
                ["obj"] = new JsonObject
                {
                    ["a"] = true,
                    ["b"] = false
                }
            };
            AreEqual(json, ParseString(json.ToString()));
        }

        [Test]
        public void PrettyFormatTest()
        {
            ImmutableJson json = new JsonObject
            {
                ["key1"] = true,
                ["key2"] = new JsonArray { 1, 2 },
                ["key3"] = new JsonObject { ["int"] = 1.1 }
            };
            var str =
@"{
    ""key1"": true,
    ""key2"": [
        1,
        2
    ],
    ""key3"": {
        ""int"": 1.1
    }
}";
            Assert.AreEqual(str.ToString(), json.ToString("4"));
        }
    }
}
