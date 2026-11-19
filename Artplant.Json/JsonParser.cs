using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Json
{
    public sealed class JsonParserException : Exception
    {
        public int Line { get; }
        public int Column { get; }
        public int Position { get; }

        public JsonParserException(string error, int position, int line, int column)
            : base(string.Format("JSON parse error: {0} at {1}:{2}", error, line, column))
        {
            Line = line;
            Column = column;
            Position = position;
        }
    }

    public sealed class JsonParser
    {
        private readonly TextReader reader;

        public int Line { get; private set; }
        public int Column { get; private set; }
        public int Position { get; private set; }

        char[] buffer = new char[1025];

        public JsonParser(TextReader reader)
        {
            this.reader = reader;
            Line = 1;
            Column = 1;
            Position = 0;
        }

        string ReadString()
        {
            int bufferPointer = 0;
            int bufferSize = buffer.Length;
            while (true)
            {
                var ch = ReadChar();
                if (ch == '\"')
                    break;

                if (bufferPointer == bufferSize)
                    bufferSize = GrowBuffer();

                if (ch == '\\')
                {
                    switch (ReadChar())
                    {
                        case '"':
                            ch = '"';
                            break;

                        case '\\':
                            ch = '\\';
                            break;

                        case '/':
                            ch = '/';
                            break;

                        case 'b':
                            ch = '\b';
                            break;

                        case 'f':
                            ch = '\f';
                            break;

                        case 'n':
                            ch = '\n';
                            break;

                        case 'r':
                            ch = '\r';
                            break;

                        case 't':
                            ch = '\t';
                            break;

                        case 'u':
                            {
                                uint n = HexCharToInt(ReadChar());
                                n = (n << 4) + HexCharToInt(ReadChar());
                                n = (n << 4) + HexCharToInt(ReadChar());
                                n = (n << 4) + HexCharToInt(ReadChar());
                                ch = (char)n;
                            }
                            break;

                        default:
                            throw Fail("Invalid escape syntax");
                    }
                }
                else if (ch < '\u001f')
                {
                    throw Fail("Character not allowed");
                }

                buffer[bufferPointer] = ch;
                bufferPointer++;
            }
            return new string(buffer, 0, bufferPointer);
        }

        string ReadNumber(char firstCharacter)
        {
            buffer[0] = firstCharacter;
            int bufferPointer = 1;
            int bufferSize = buffer.Length;
            bool done = false;
            while (!done)
            {
                int i = reader.Peek();
                if (i < 0)
                    break;

                if (bufferPointer == bufferSize)
                    bufferSize = GrowBuffer();

                var ch = (char)i;

                switch (ch)
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                    case '.':
                    case '-':
                    case '+':
                    case 'e':
                    case 'E':
                        ReadChar();
                        buffer[bufferPointer] = ch;
                        bufferPointer++;
                        break;

                    default:
                        done = true;
                        break;
                }
            }
            var str = new string(buffer, 0, bufferPointer);
            if (!double.TryParse(str, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var val))
                throw Fail("Invalid number");
            return str;
        }

        void ExpectNull()
        {
            var c1 = ReadChar();
            var c2 = ReadChar();
            var c3 = ReadChar();
            if (c1 != 'u' || c2 != 'l' || c3 != 'l')
                throw Fail("Invalid token");
        }

        void ExpectTrue()
        {
            var c1 = ReadChar();
            var c2 = ReadChar();
            var c3 = ReadChar();
            if (c1 != 'r' || c2 != 'u' || c3 != 'e')
                throw Fail("Invalid token");
        }

        void ExpectFalse()
        {
            var c1 = ReadChar();
            var c2 = ReadChar();
            var c3 = ReadChar();
            var c4 = ReadChar();
            if (c1 != 'a' || c2 != 'l' || c3 != 's' || c4 != 'e')
                throw Fail("Invalid token");
        }

        int GrowBuffer()
        {
            var newSize = buffer.Length * 2;
            Array.Resize(ref buffer, newSize);
            return newSize;
        }

        char ReadChar()
        {
            int i = reader.Read();
            if (i < 0)
                throw Fail("Unexpected end of string");
            var c = (char)i;
            Column++;
            Position++;
            if (c == '\n')
            {
                Line++;
                Column = 1;
            }
            return c;
        }

        char ReadNonSpaceChar()
        {
            char ch = ' ';
            while (ch == ' ' || ch == '\r' || ch == '\r' || ch == '\n' || ch == '\t')
                ch = ReadChar();
            return ch;
        }

        JsonParserException Fail(string error)
        {
            return new JsonParserException(error, Position, Line, Column);
        }

        uint HexCharToInt(char ch)
        {
            switch (ch)
            {
                case '0':
                    return 0;
                case '1':
                    return 1;
                case '2':
                    return 2;
                case '3':
                    return 3;
                case '4':
                    return 4;
                case '5':
                    return 5;
                case '6':
                    return 6;
                case '7':
                    return 7;
                case '8':
                    return 8;
                case '9':
                    return 9;
                case 'a':
                case 'A':
                    return 10;
                case 'b':
                case 'B':
                    return 11;
                case 'c':
                case 'C':
                    return 12;
                case 'd':
                case 'D':
                    return 13;
                case 'e':
                case 'E':
                    return 14;
                case 'f':
                case 'F':
                    return 15;
                default:
                    throw Fail("Invalid hex char " + ch);
            }
        }

        public string? LastTokenString { get; private set; }

        public JsonToken ReadToken()
        {
            char ch = ReadNonSpaceChar();
            switch (ch)
            {
                case '[':
                    return JsonToken.ArrayStart;

                case ',':
                    return JsonToken.Comma;

                case ']':
                    return JsonToken.ArrayEnd;

                case '{':
                    return JsonToken.ObjectStart;

                case '}':
                    return JsonToken.ObjectEnd;

                case ':':
                    return JsonToken.KeyValueSeparator;

                case '\"':
                    LastTokenString = ReadString();
                    return JsonToken.String;

                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '-':
                    LastTokenString = ReadNumber(ch);
                    return JsonToken.Number;

                case 'n':
                    ExpectNull();
                    return JsonToken.Null;

                case 't':
                    ExpectTrue();
                    return JsonToken.True;

                case 'f':
                    ExpectFalse();
                    return JsonToken.False;

                default:
                    throw Fail("Unexpected character " + ch);
            }
        }

        public ImmutableJson ReadValue()
        {
            return ReadValue(ReadToken());
        }

        public ImmutableJson ReadSingleValue()
        {
            var value = ReadValue(ReadToken());
            var toEnd = reader.ReadToEnd().Trim(' ', '\r', '\r', '\n','\t');
            if (toEnd != "")
                throw Fail("Unexpected character " + toEnd[0]);
            return value;
        }

        ImmutableJson ReadValue(JsonToken firstToken)
        {
            switch (firstToken)
            {
                case JsonToken.Null:
                    return ImmutableJson.Null;

                case JsonToken.True:
                    return ImmutableJson.True;

                case JsonToken.False:
                    return ImmutableJson.False;

                case JsonToken.String:
                    return LastTokenString!;

                case JsonToken.Number:
                    return ParseNumber(LastTokenString!);

                case JsonToken.ArrayStart:
                    {
                        List<ImmutableJson>? instance = null;
                        while (true)
                        {
                            var token = ReadToken();
                            if (token == JsonToken.ArrayEnd)
                                break;

                            if (instance == null)
                                instance = new List<ImmutableJson>();

                            instance.Add(ReadValue(token));

                            JsonToken nextToken = ReadToken();
                            if (nextToken == JsonToken.ArrayEnd)
                                break;
                            else if (nextToken != JsonToken.Comma)
                                throw Fail("Expected array separator");
                        }
                        return instance == null ? ImmutableJson.EmptyArray : new ImmutableJsonArray(instance);
                    }

                case JsonToken.ObjectStart:
                    {
                        Dictionary<string, ImmutableJson>? instance = null;
                        while (true)
                        {
                            var token = ReadToken();
                            if (token == JsonToken.ObjectEnd)
                                break;
                            if (token != JsonToken.String)
                                throw Fail("Property name should be a string.");

                            var propertyName = LastTokenString!;

                            if (ReadToken() != JsonToken.KeyValueSeparator)
                                throw Fail("Invalid key/value separator.");

                            var propertyValue = ReadValue();

                            if (instance != null && instance.ContainsKey(propertyName))
                                throw Fail("Duplicate key " + propertyName);

                            if (instance == null)
                                instance = new Dictionary<string, ImmutableJson>();

                            instance.Add(propertyName, propertyValue);

                            var nextToken = ReadToken();
                            if (nextToken == JsonToken.ObjectEnd)
                                break;
                            else if (nextToken != JsonToken.Comma)
                                throw Fail("Invalid object pairs separator");
                        }
                        return instance == null ? ImmutableJson.EmptyObject : new ImmutableJsonObject(instance);
                    }

                default:
                    throw Fail("Unexpected token " + firstToken);
            }
        }

        public static ImmutableJson Parse(TextReader reader)
        {
            return new JsonParser(reader).ReadSingleValue();
        }

        public static ImmutableJson Parse(string json)
        {
            using (var reader = new StringReader(json))
            {
                return new JsonParser(reader).ReadSingleValue();
            }
        }

        static ImmutableJson ParseNumber(string value)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var res))
                return new ImmutableJsonInteger(res);
            if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var lres))
                return new ImmutableJsonLong(lres);
            return new ImmutableJsonDouble(double.Parse(value, CultureInfo.InvariantCulture));
        }
    }
}