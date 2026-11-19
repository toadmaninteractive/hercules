using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Hercules
{

    public static class JsonSettingsOptions
    {
        public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General)
        {
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public class JsonSettingsReader : ISettingsReader
    {
        private readonly JsonDocument jsonDocument;

        public JsonSettingsReader(string filename)
        {
            using (var file = File.OpenRead(filename))
                jsonDocument = JsonDocument.Parse(file);
        }

        public void Dispose()
        {
            jsonDocument.Dispose();
        }

        public bool Read<T>(string name, [MaybeNullWhen(returnValue: false)] out T value)
        {
            if (jsonDocument.RootElement.TryGetProperty(name, out var jsonValue))
            {
                value = JsonSerializer.Deserialize<T>(jsonValue, JsonSettingsOptions.Options)!;
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
            jsonObject[name] = JsonSerializer.SerializeToNode(value, JsonSettingsOptions.Options);
        }

        public void Save(string filename)
        {
            File.WriteAllText(filename, jsonObject.ToJsonString());
        }
    }
}
