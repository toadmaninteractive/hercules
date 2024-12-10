using System.IO;

namespace Json
{
    public class JsonLocation
    {
        public int Line { get; }
        public int Column { get; }
        public int Position { get; }

        public static JsonLocation Parser(JsonParser parser)
        {
            return new(parser.Line, parser.Column, parser.Position);
        }

        public static JsonLocation ParserException(JsonParserException exception)
        {
            return new(exception.Line, exception.Column, exception.Position);
        }

        public JsonLocation(int line, int column, int position)
        {
            Line = line;
            Column = column;
            Position = position;
        }
    }

    public static class GotoJsonPath
    {
        public static JsonLocation? FindPosition(string jsonString, JsonPath path)
        {
            try
            {
                using var reader = new StringReader(jsonString);
                var parser = new JsonParser(reader);
                while (path.Head != null)
                {
                    var node = path.Head;
                    path = path.Tail;
                    if (node is JsonObjectPathNode objectPathNode)
                    {
                        parser.ReadToken();
                        parser.ReadToken();
                        while (parser.LastTokenString != objectPathNode.Key)
                        {
                            parser.ReadToken();
                            parser.ReadValue();
                            parser.ReadToken();
                            parser.ReadToken();
                        }
                        parser.ReadToken();
                    }
                    else if (node is JsonObjectKeyPathNode objectKeyPathNode)
                    {
                        parser.ReadToken();
                        var pos = JsonLocation.Parser(parser);
                        parser.ReadToken();
                        while (parser.LastTokenString != objectKeyPathNode.Key)
                        {
                            parser.ReadToken();
                            parser.ReadValue();
                            parser.ReadToken();
                            pos = JsonLocation.Parser(parser);
                            parser.ReadToken();
                        }
                        return pos;
                    }
                    else if (node is JsonArrayPathNode arrayPathNode)
                    {
                        parser.ReadToken();
                        for (int i = 0; i < arrayPathNode.Index; i++)
                        {
                            parser.ReadValue();
                            parser.ReadToken();
                        }
                    }
                }
                return JsonLocation.Parser(parser);
            }
            catch
            {
                return null;
            }
        }
    }
}
