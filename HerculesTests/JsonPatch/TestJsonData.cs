using Json;
using System.Collections.Generic;
using System.IO;

namespace Hercules.Json.Tests
{
    public static class TestJsonValue
    {
        public static string JsonStringExample => GetResourceString("Hercules.Resources.Json.Example.json");

        public static ImmutableJson GetResourceJson(string name)
        {
            return JsonParser.Parse(GetResourceString(name));
        }

        public static string GetResourceString(string name)
        {
            using var stream = typeof(TestJsonValue).Assembly.GetManifestResourceStream(name);
            using var reader = new StreamReader(stream!);
            return reader.ReadToEnd();
        }

        public static IEnumerable<ImmutableJson> JsonValues
        {
            get
            {
                yield return ImmutableJson.Null;
                yield return true;
                yield return false;
                yield return new JsonArray();
                yield return new JsonObject();
                yield return "test";
                yield return 19.1;
                yield return new JsonArray() { ImmutableJson.True, ImmutableJson.False };
                yield return new JsonObject() { { "true", ImmutableJson.True }, { "false", ImmutableJson.False } };
                yield return JsonParser.Parse(JsonStringExample);
            }
        }
    }
}
