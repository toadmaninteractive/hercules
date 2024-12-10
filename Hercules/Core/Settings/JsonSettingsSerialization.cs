using Json;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Hercules
{
    public class JsonSettingsReader : ISettingsReader
    {
        private readonly ImmutableJsonObject jsonObject;

        public JsonSettingsReader(string filename)
        {
            jsonObject = FileUtils.LoadJsonFromFile(filename).AsObject;
        }

        public bool Read<T>(string name, [MaybeNullWhen(returnValue: false)] out T value)
        {
            if (jsonObject.TryGetValue(name, out var jsonValue))
            {
                var serializer = Json.Serialization.JsonDefaultSerializer.Create<T>();
                value = serializer.Deserialize(jsonValue);
                return true;
            }
            value = default!;
            return false;
        }
    }

    public class JsonSettingsWriter : ISettingsWriter
    {
        readonly JsonObject jsonObject = new JsonObject();

        public void Write<T>(string name, T value)
        {
            jsonObject[name] = Json.Serialization.JsonDefaultSerializer.Create<T>().Serialize(value);
        }

        public void Save(string filename)
        {
            File.WriteAllText(filename, jsonObject.ToString("4"));
        }
    }
}
