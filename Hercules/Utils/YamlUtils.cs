using Json;
using System.Text;
using VYaml.Parser;

namespace Hercules
{
    public static class YamlUtils
    {
        public static ImmutableJson ParseYaml(string text)
        {
            var utf8 = Encoding.UTF8.GetBytes(text);
            var parser = YamlParser.FromBytes(utf8);

            var jsonArray = new JsonArray();
            while (!parser.End)
            {
                parser.SkipAfter(ParseEventType.DocumentStart);
                if (parser.CurrentEventType == ParseEventType.StreamEnd)
                    break;
                parser.TryGetCurrentTag(out var tag);
                parser.TryGetCurrentAnchor(out var anchor);
                var document = ParseNode(ref parser).AsObject.ToMutable();
                if (tag != null || anchor != null)
                {
                    var meta = new JsonObject();
                    if (tag != null)
                    {
                        meta.Add("tag", tag.ToString());
                    }
                    if (anchor != null)
                    {
                        meta.Add("anchor", anchor.Name);
                    }
                    document.Add("yaml", meta);
                }
                jsonArray.Add(document);
            }
            return jsonArray.ToImmutable();
        }

        private static ImmutableJson ParseNode(ref YamlParser parser)
        {
            switch (parser.CurrentEventType)
            {
                case ParseEventType.MappingStart:
                    parser.Read(); // consume start
                    var map = new JsonObject();
                    while (parser.CurrentEventType != ParseEventType.MappingEnd)
                    {
                        var key = parser.ReadScalarAsString()!;
                        var value = ParseNode(ref parser);
                        map.Add(key, value);
                    }
                    parser.Read(); // consume end
                    return map;

                case ParseEventType.SequenceStart:
                    parser.Read(); // consume start
                    var list = new JsonArray();
                    while (parser.CurrentEventType != ParseEventType.SequenceEnd)
                    {
                        list.Add(ParseNode(ref parser));
                    }
                    parser.Read(); // consume end
                    return list;

                case ParseEventType.Scalar:
                    ImmutableJson jsonScalar;
                    if (parser.TryGetScalarAsInt32(out var intValue))
                    {
                        jsonScalar = intValue;
                    }
                    else if (parser.TryGetScalarAsFloat(out var floatValue) && parser.GetScalarAsString()!.Contains('.'))
                    {
                        jsonScalar = floatValue;
                    }
                    else if (parser.TryGetScalarAsString(out var stringValue) && stringValue != null)
                    {
                        jsonScalar = stringValue;
                    }
                    else
                    {
                        jsonScalar = ImmutableJson.Null;
                    }
                    parser.Read();
                    return jsonScalar;

                default:
                    parser.SkipCurrentNode();
                    return ImmutableJson.Null;
            }
        }
    }
}
