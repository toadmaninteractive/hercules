using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Json
{
    public sealed class JsonFormat : IFormatProvider
    {
        public static readonly JsonFormat Compact = new JsonFormat(isMultiline: false);
        public static readonly JsonFormat Multiline = new JsonFormat(isMultiline: true, indentString: "    ");

        public bool IsMultiline { get; }
        public string IndentString { get; }
        public string? LinePrefix { get; }

        public JsonFormat(bool isMultiline = false, string indentString = "    ", string? linePrefix = null)
        {
            this.IsMultiline = isMultiline;
            this.IndentString = indentString;
            this.LinePrefix = linePrefix;
        }

        public static JsonFormat GetInstance(string? format, IFormatProvider? formatProvider)
        {
            if (formatProvider != null)
            {
                if (formatProvider.GetFormat(typeof(JsonFormat)) is JsonFormat jsonFormat)
                    return jsonFormat;
            }

            switch (format)
            {
                case null:
                case "G":
                case "g":
                    return Compact;

                case "T":
                    return new JsonFormat(true, "\t");

                case "1":
                    return new JsonFormat(true, " ");

                case "2":
                    return new JsonFormat(true, "  ");

                case "3":
                    return new JsonFormat(true, "   ");

                case "4":
                    return new JsonFormat(true, "    ");

                default:
                    return Compact;
            }
        }

        public object? GetFormat(Type? formatType)
        {
            return formatType == typeof(JsonFormat) ? this : null;
        }

        public string Format(ImmutableJson json)
        {
            var builder = new StringBuilder();
            Format(json, builder, 0, true, true);
            return builder.ToString();
        }

        public string Format(JsonObject json)
        {
            var builder = new StringBuilder();
            NewLine(builder, 0, true);
            FormatObject(json.GetData(false), builder, 0);
            return builder.ToString();
        }

        public string Format(JsonArray json)
        {
            var builder = new StringBuilder();
            NewLine(builder, 0, true);
            FormatArray(json.GetData(false), builder, 0);
            return builder.ToString();
        }

        void Format(ImmutableJson json, StringBuilder builder, int indent, bool newLine, bool firstLine = false)
        {
            if (newLine)
                NewLine(builder, indent, firstLine);

            switch (json.Type)
            {
                case JsonType.Null:
                case JsonType.Boolean:
                case JsonType.Number:
                    builder.Append(json.ToString());
                    break;
                case JsonType.String:
                    builder.AppendFormat("\"{0}\"", EscapeString(json.AsString));
                    break;
                case JsonType.Array:
                    FormatArray(json.AsArray, builder, indent);
                    break;
                case JsonType.Object:
                    FormatObject(json.AsObject, builder, indent);
                    break;
            }
        }

        void FormatArray(IReadOnlyList<ImmutableJson> items, StringBuilder builder, int indent)
        {
            builder.Append('[');
            for (var i = 0; i < items.Count; i++)
            {
                Format(items[i], builder, indent + 1, true);
                if (i < items.Count - 1)
                    builder.Append(',');
            }
            NewLine(builder, indent);
            builder.Append(']');
        }

        void FormatObject(IReadOnlyDictionary<string, ImmutableJson> dict, StringBuilder builder, int indent)
        {
            builder.Append('{');
            var c = dict.Count;
            foreach (var pair in dict)
            {
                NewLine(builder, indent + 1);
                builder.Append('"');
                builder.Append(pair.Key);
                builder.Append(IsMultiline ? "\": " : "\":");
                Format(pair.Value, builder, indent + 1, false);
                if (c > 1)
                    builder.Append(',');
                c--;
            }
            NewLine(builder, indent);
            builder.Append('}');
        }

        void NewLine(StringBuilder builder, int indent, bool firstLine = false)
        {
            if (!IsMultiline)
                return;
            if (!firstLine)
                builder.AppendLine();
            if (!string.IsNullOrEmpty(LinePrefix))
                builder.Append(LinePrefix);
            for (var i = 0; i < indent; i++)
                builder.Append(IndentString);
        }

        string EscapeString(string str)
        {
            // TODO: use JSON standard
            return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\t", "\\t").Replace("\r", "");
        }

        public static string Format(string str, string indentString = "    ", string? prefix = null)
        {
            var indent = 0;
            var quoted = false;
            var sb = new StringBuilder();
            if (prefix != null)
                sb.Append(prefix);
            for (var i = 0; i < str.Length; i++)
            {
                var ch = str[i];
                switch (ch)
                {
                    case '{':
                    case '[':
                        sb.Append(ch);
                        if (!quoted)
                        {
                            indent++;
                            sb.AppendLine();
                            if (prefix != null)
                                sb.Append(prefix);
                            for (int ind = 0; ind < indent; ind++)
                                sb.Append(indentString);
                        }
                        break;
                    case '}':
                    case ']':
                        if (!quoted)
                        {
                            indent--;
                            sb.AppendLine();
                            if (prefix != null)
                                sb.Append(prefix);
                            for (int ind = 0; ind < indent; ind++)
                                sb.Append(indentString);
                        }
                        sb.Append(ch);
                        break;
                    case '"':
                        sb.Append(ch);
                        bool escaped = false;
                        var index = i;
                        while (index > 0 && str[--index] == '\\')
                            escaped = !escaped;
                        if (!escaped)
                            quoted = !quoted;
                        break;
                    case ',':
                        sb.Append(ch);
                        if (!quoted)
                        {
                            sb.AppendLine();
                            if (prefix != null)
                                sb.Append(prefix);
                            for (int ind = 0; ind < indent; ind++)
                                sb.Append(indentString);
                        }
                        break;
                    case ':':
                        sb.Append(ch);
                        if (!quoted)
                            sb.Append(" ");
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
