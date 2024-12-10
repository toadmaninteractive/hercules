using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Hercules.Import
{
    public class ExcelImportTable
    {
        public RawTable LoadFromFile(string fileName)
        {
            var table = new RawTable();

            using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                IWorkbook workbook = WorkbookFactory.Create(stream);
                ISheet sheet = workbook.GetSheetAt(0);

                for (int j = sheet.FirstRowNum; j <= sheet.LastRowNum; j++)
                {
                    var strings = new List<string>();
                    var row = sheet.GetRow(j);
                    for (int i = row.FirstCellNum; i < row.LastCellNum; i++)
                    {
                        var cell = row.GetCell(i);
                        if (cell == null)
                            strings.Add(string.Empty);
                        else
                        {
                            var cellType = cell.CellType;
                            if (cellType == CellType.Formula)
                                cellType = cell.CachedFormulaResultType;
                            switch (cellType)
                            {
                                case CellType.String:
                                    strings.Add(cell.StringCellValue);
                                    break;

                                case CellType.Boolean:
                                    strings.Add(cell.BooleanCellValue.ToString());
                                    break;

                                case CellType.Numeric:
                                    strings.Add(cell.NumericCellValue.ToString(CultureInfo.InvariantCulture));
                                    break;

                                default:
                                    strings.Add(string.Empty);
                                    break;
                            }
                        }
                    }
                    table.AddRow(strings);
                }
            }

            var rowCount = table.Rows.Count;
            if (rowCount == 0)
                throw new Exception("Table does not contain any rows");
            var colCount = table.ColumnCount;
            if (colCount == 0)
                throw new Exception("Table does not contain any columns");
            return table;
        }
    }
}
