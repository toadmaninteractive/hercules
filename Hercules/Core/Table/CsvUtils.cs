using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Hercules
{
    public static class CsvUtils
    {
        static CsvConfiguration AutoDetectDelimiter(TextReader reader)
        {
            var line = reader.ReadLine();
            if (line == null)
                return new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = "\t" };
            if (line.StartsWith("sep=", StringComparison.OrdinalIgnoreCase))
            {
                return new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = line.Substring(4) };
            }
            var delimiter = GetSeparator(line) ?? '\t';
            return new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = delimiter.ToString(CultureInfo.InvariantCulture) };
        }

        public static char? GetSeparator(string line)
        {
            var i = 0;
            while (true)
            {
                i = line.IndexOfAny(new[] { '\t', ';', ',', '"' }, i);
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

        public static IReadOnlyList<IReadOnlyList<string>> LoadFromString(string text)
        {
            CsvConfiguration config;
            using (var stringReader = new StringReader(text))
                config = AutoDetectDelimiter(stringReader);
            config.HasHeaderRecord = false;
            config.BadDataFound = null;
            using (var stringReader = new StringReader(text))
                return LoadFromText(stringReader, config);
        }

        public static IReadOnlyList<IReadOnlyList<string>> LoadFromFile(string filename)
        {
            CsvConfiguration config;
            using var reader = new StreamReader(filename);
            config = AutoDetectDelimiter(reader);
            config.HasHeaderRecord = false;
            config.BadDataFound = null;
            reader.BaseStream.Position = 0;
            reader.DiscardBufferedData();
            return LoadFromText(reader, config);
        }

        private static IReadOnlyList<IReadOnlyList<string>> LoadFromText(TextReader reader, CsvConfiguration config)
        {
            var result = new List<IReadOnlyList<string>>();
            using (var csv = new CsvParser(reader, config))
            {
                while (csv.Read())
                    result.Add(csv.Record);
            }
            return result;
        }

        public static void SaveToFile(IEnumerable<IEnumerable<string>> rows, string filename, char delimiter)
        {
            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter.ToString(CultureInfo.InvariantCulture),
                HasHeaderRecord = false,
            };

            using StreamWriter sw = new StreamWriter(filename, false);
            SaveToStream(rows, sw, configuration);
        }

        public static string SaveToString(IEnumerable<IEnumerable<string>> rows, char delimiter)
        {
            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter.ToString(CultureInfo.InvariantCulture),
                HasHeaderRecord = false,
            };

            using StringWriter sw = new StringWriter();
            SaveToStream(rows, sw, configuration);
            return sw.ToString();
        }

        static void SaveToStream(IEnumerable<IEnumerable<string>> rows, TextWriter writer, CsvConfiguration configuration)
        {
            using var csv = new CsvWriter(writer, configuration);
            foreach (var row in rows)
            {
                foreach (var item in row)
                {
                    csv.WriteField(item);
                }
                csv.NextRecord();
            }
        }
    }
}
