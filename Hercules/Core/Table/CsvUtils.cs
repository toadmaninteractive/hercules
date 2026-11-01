using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Hercules
{
    public static class CsvUtils
    {
        static char AutoDetectDelimiter(TextReader reader)
        {
            var line = reader.ReadLine();
            if (line == null)
                return '	';
            if (line.StartsWith("sep=", StringComparison.OrdinalIgnoreCase) && line.Length > 4)
                return line[4];
            return GetSeparator(line) ?? '	';
        }

        public static char? GetSeparator(string line)
        {
            var i = 0;
            while (true)
            {
                i = line.IndexOfAny(['\t', ';', ',', '"'], i);
                if (i < 0)
                    return null;
                var c = line[i];
                if (c == '"')
                {
                    i = line.IndexOf('"', i + 1);
                    if (i < 0 || i == line.Length - 1)
                        return null;
                    i++;
                }
                else
                    return c;
            }
        }

        private static CsvReaderOptions CreateReaderOptions(char delimiter) => new()
        {
            Separator = delimiter,
            HasHeader = false,
            FormatProvider = CultureInfo.InvariantCulture,
            MalformedData = CsvMalformedDataHandling.Recover,
            FieldCountMismatch = CsvFieldCountHandling.Allow,
        };

        public static IReadOnlyList<IReadOnlyList<string>> LoadFromString(string text)
        {
            char delimiter;
            using (var stringReader = new StringReader(text))
                delimiter = AutoDetectDelimiter(stringReader);
            using (var stringReader = new StringReader(text))
                return LoadFromText(stringReader, CreateReaderOptions(delimiter));
        }

        public static IReadOnlyList<IReadOnlyList<string>> LoadFromFile(string filename)
        {
            using var reader = new StreamReader(filename);
            var delimiter = AutoDetectDelimiter(reader);
            reader.BaseStream.Position = 0;
            reader.DiscardBufferedData();
            return LoadFromText(reader, CreateReaderOptions(delimiter));
        }

        private static IReadOnlyList<IReadOnlyList<string>> LoadFromText(TextReader reader, CsvReaderOptions options)
        {
            var result = new List<IReadOnlyList<string>>();
            using (var csv = CsvReader.Create(reader, options))
            {
                while (csv.Read())
                    result.Add(csv.Current.ToArray());
            }
            return result;
        }

        internal static CsvWriterOptions CreateWriterOptions(char delimiter) => new()
        {
            Separator = delimiter,
            HasHeader = false,
            ValidateFieldCount = false,
            FormatProvider = CultureInfo.InvariantCulture,
        };

        public static void SaveToFile(IEnumerable<IEnumerable<string>> rows, string filename, char delimiter)
        {
            var configuration = CreateWriterOptions(delimiter);

            using StreamWriter sw = new StreamWriter(filename, false);
            SaveToStream(rows, sw, configuration);
        }

        public static string SaveToString(IEnumerable<IEnumerable<string>> rows, char delimiter)
        {
            var configuration = CreateWriterOptions(delimiter);

            using StringWriter sw = new StringWriter();
            SaveToStream(rows, sw, configuration);
            return sw.ToString();
        }

        static void SaveToStream(IEnumerable<IEnumerable<string>> rows, TextWriter writer, CsvWriterOptions configuration)
        {
            using var csv = CsvWriter.Create(writer, configuration);
            foreach (var row in rows)
            {
                foreach (var item in row)
                {
                    csv.WriteField(item);
                }
                csv.EndRecord();
            }
        }
    }
}
