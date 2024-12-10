using System;
using System.Collections.Generic;

namespace Hercules.Import
{
    public static class CsvImportTable
    {
        private static RawTable LoadFromCsv(IEnumerable<IReadOnlyList<string>> csv)
        {
            var table = new RawTable();
            foreach (var record in csv)
                table.AddRow(record);

            var rowCount = table.Rows.Count;
            if (rowCount == 0)
                throw new Exception("Table does not contain any rows");
            var colCount = table.ColumnCount;
            if (colCount == 0)
                throw new Exception("Table does not contain any columns");
            return table;
        }

        public static RawTable LoadFromText(string text)
        {
            return LoadFromCsv(CsvUtils.LoadFromString(text));
        }

        public static RawTable LoadFromFile(string fileName)
        {
            return LoadFromCsv(CsvUtils.LoadFromFile(fileName));
        }
    }
}
