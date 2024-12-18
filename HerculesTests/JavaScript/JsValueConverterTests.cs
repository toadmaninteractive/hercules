using Hercules.Json.Tests;
using Jint;
using Json;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Hercules.Scripting.JavaScript.Tests
{
    [TestFixture]
    public class JsValueConverterTests
    {
        [Test]
        public void FromJsonValue([ValueSource(typeof(TestJsonValue), nameof(TestJsonValue.JsonValues))] ImmutableJson json)
        {
            var engine = new Engine();
            var value = JsValueConverter.FromJsonValue(json, engine);
            var json1 = JsValueConverter.ToJsonValue(value);
            ClassicAssert.AreEqual(json, json1);
        }
    }
}
