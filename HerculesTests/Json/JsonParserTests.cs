using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Json.Tests
{
    [TestFixture]
    public class JsonParserTests
    {
        static object[] ParseCases =
        {
            new object[] { JsonExamples.JsonExampleString1, JsonExamples.JsonExample1 },
            new object[] { JsonExamples.JsonExampleString2, JsonExamples.JsonExample2 },
            new object[] { JsonExamples.JsonExampleString3, JsonExamples.JsonExample3 },

            new object[] { "null", ImmutableJson.Null },
            new object[] { "156", ImmutableJson.Create(156) },
            new object[] { "-9", ImmutableJson.Create(-9) },
            new object[] { "5.123", ImmutableJson.Create(5.123) },
            new object[] { "1.23456789123457E+17", ImmutableJson.Create(1.23456789123457E+17) },
            new object[] { "1527519502732", ImmutableJson.Create(1527519502732L) },
            new object[] { "true", ImmutableJson.True },
            new object[] { "false", ImmutableJson.False },
            new object[] { @"""test""", ImmutableJson.Create("test") },
            new object[] { "[]", ImmutableJson.EmptyArray },
            new object[] { "{}", ImmutableJson.EmptyObject },
        };

        [TestCaseSource("ParseCases")]
        public void ParseTest(string str, ImmutableJson json)
        {
            Assert.AreEqual(json, JsonParser.Parse(str));
        }
    }
}
