using System;
using System.IO;
using System.Text;

namespace Hercules
{
    public static class StringUtils
    {
        public static string Truncate(this string value, int maxLength, string suffix = "...")
        {
            if (value.Length > maxLength)
                return value.Substring(0, maxLength - suffix.Length) + suffix;
            else
                return value;
        }

        public static string TryReplacePrefix(this string value, string prefix, string substitute, StringComparison stringComparison)
        {
            if (value.StartsWith(prefix, stringComparison))
                return substitute + value.Substring(prefix.Length);
            else
                return value;
        }

        public static string RemoveSuffix(this string value, string suffix, StringComparison comparisonType = StringComparison.Ordinal)
        {
            if (value.EndsWith(suffix, comparisonType))
                return value[..^suffix.Length];
            else
                return value;
        }

        public static string RemovePrefix(this string value, string prefix, StringComparison comparisonType = StringComparison.Ordinal)
        {
            if (value.StartsWith(prefix, comparisonType))
                return value.Substring(prefix.Length);
            else
                return value;
        }

        static readonly char[] WordDelimiterChars = { ' ', ',', '.', ':', ';', '\t' };

        public static bool SmartFilter(this string value, string filter)
        {
            if (value.Contains(filter, StringComparison.OrdinalIgnoreCase))
                return true;
            if (filter.Contains(' ', StringComparison.Ordinal) && value.IndexOfAny(WordDelimiterChars) >= 0)
            {
                var words = value.Split(WordDelimiterChars, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var filterWords = filter.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                int i = 0;
                int j = 0;
                while (j < filterWords.Length && i < words.Length)
                {
                    var filterWord = filterWords[j];
                    var word = words[i];
                    i++;
                    if (word.Contains(filterWord, StringComparison.OrdinalIgnoreCase))
                    {
                        j++;
                    }
                }

                if (j == filterWords.Length)
                    return true;
            }
            return false;
        }
    }

    public class StringWriterWithEncoding : StringWriter
    {
        public override Encoding Encoding { get; }

        public StringWriterWithEncoding(Encoding encoding) : base()
        {
            this.Encoding = encoding;
        }
    }
}
