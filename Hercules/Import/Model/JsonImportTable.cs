using Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Hercules.Import
{
    public class JsonImportTable
    {
        public static RawTable LoadFromFile(string fileName)
        {
            using var reader = new StreamReader(fileName);
            return LoadJson(JsonParser.Parse(reader));
        }

        public static RawTable LoadFromText(string jsonText)
        {
            return LoadJson(JsonParser.Parse(jsonText));
        }

        public static RawTable LoadJson(ImmutableJson json)
        {
            var table = new RawTable();
            var array = json.AsArray;
            var names = array.Where(item => item.IsObject).SelectMany(item => item.AsObject.Keys).Distinct().ToList();
            table.AddRow(names);
            var row = new List<string?>(names.Count);
            foreach (var item in array)
            {
                if (item.IsObject)
                {
                    row.Clear();
                    var obj = item.AsObject;
                    foreach (var name in names)
                    {
                        if (obj.TryGetValue(name, out var value))
                            row.Add(JsonToString(value));
                        else
                            row.Add(null);
                    }
                    table.AddRow(row);
                }
            }
            return table;
        }

        private static string JsonToString(ImmutableJson json)
        {
            return json switch
            {
                ImmutableJsonString s => s.Value,
                _ => json.ToString(),
            };
        }
    }
}
