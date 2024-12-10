using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Globalization;
using System.IO;

namespace Hercules
{
    public record CsvExporter(char Delimiter, TimeZoneInfo TimeZone, string DateTimeFormat) : ITableExporter
    {
        public string ExportToString(IExportTable table)
        {
            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = Delimiter.ToString(CultureInfo.InvariantCulture),
                HasHeaderRecord = false,
            };

            using StringWriter sw = new StringWriter();
            SaveToStream(table, sw, configuration);
            return sw.ToString();
        }

        public void Export(IExportTable table, string fileName)
        {
            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = Delimiter.ToString(CultureInfo.InvariantCulture),
                HasHeaderRecord = false,
            };

            using StreamWriter sw = new StreamWriter(fileName, false);
            SaveToStream(table, sw, configuration);
        }

        private void SaveToStream(IExportTable table, TextWriter writer, CsvConfiguration configuration)
        {
            using var csv = new CsvWriter(writer, configuration);
            foreach (var col in table.ExportColumns)
            {
                csv.WriteField(col.Name);
            }
            csv.NextRecord();

            for (int row = 0; row < table.RowCount; row++)
            {
                foreach (var col in table.ExportColumns)
                {
                    var value = table.GetExportValue(row, col);

                    var asString = value switch
                    {
                        null => string.Empty,
                        string stringValue => stringValue,
                        double doubleValue => doubleValue.ToString(CultureInfo.InvariantCulture),
                        float floatValue => floatValue.ToString(CultureInfo.InvariantCulture),
                        int intValue => intValue.ToString(CultureInfo.InvariantCulture),
                        bool boolValue => boolValue ? "true" : "false",
                        DateTime dateTimeValue => TimeZoneInfo.ConvertTimeFromUtc(dateTimeValue, TimeZone).ToString(DateTimeFormat),
                        _ => value.ToString()!.Replace(Environment.NewLine, "\n", StringComparison.Ordinal).Replace("\n", Environment.NewLine, StringComparison.Ordinal),
                    };

                    csv.WriteField(asString);
                }
                csv.NextRecord();
            }
        }
    }
}
