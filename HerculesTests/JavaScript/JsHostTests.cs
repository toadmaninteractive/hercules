using Hercules.Json.Tests;
using Json;
using NUnit.Framework;

namespace Hercules.Scripting.JavaScript.Tests
{
    [TestFixture]
    public class JsHostTests
    {
        [Test]
        public void GetJsonValue_Undefined_IsNull()
        {
            var result = new JsHost().GetJsonValue("not_exists");
            Assert.That(result, Is.EqualTo(ImmutableJson.Null));
        }

        [Test]
        public void GetSetJsonValue([ValueSource(typeof(TestJsonValue), nameof(TestJsonValue.JsonValues))] ImmutableJson json)
        {
            var result = new JsHost().SetJsonValue("value", json).GetJsonValue("value");
            Assert.That(result, Is.EqualTo(json));
        }

        [Test]
        public void Execute()
        {
            var result = new JsHost().Execute("var i=2+2;").GetJsonValue("i");
            Assert.That(result, Is.EqualTo(ImmutableJson.Create(4)));
        }
    }
}
