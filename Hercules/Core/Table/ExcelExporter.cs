using NPOI;
using NPOI.HPSF;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Globalization;
using System.IO;
using System.Threading;

namespace Hercules
{
    public enum ExcelFormat
    {
        Excel2003,
        Excel2007,
    }

    public record ExcelExporter(ExcelFormat Format, string Sheet, TimeZoneInfo TimeZone, string DateTimeFormat) : ITableExporter
    {
        public void Export(IExportTable table, string fileName)
        {
            switch (Format)
            {
                case ExcelFormat.Excel2003:
                    ExportToExcel2003(table, fileName);
                    break;

                case ExcelFormat.Excel2007:
                    ExportToExcel2007(table, fileName);
                    break;
            }
        }

        private void ExportToExcel2003(IExportTable table, string fileName)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));

            var currentCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                var workbook = new HSSFWorkbook();

                DocumentSummaryInformation dsi = PropertySetFactory.CreateDocumentSummaryInformation();
                dsi.Company = "Toadman Interactive";
                workbook.DocumentSummaryInformation = dsi;

                SummaryInformation si = PropertySetFactory.CreateSummaryInformation();
                si.Subject = "Hercules Summary";
                si.ApplicationName = "Hercules";
                workbook.SummaryInformation = si;

                ISheet sheet = workbook.CreateSheet(Sheet);
                ExportToSheet(table, sheet);

                using var file = new FileStream(fileName, FileMode.Create);
                workbook.Write(file);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = currentCulture;
            }
        }

        private void ExportToExcel2007(IExportTable table, string fileName)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));

            var currentCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                var workbook = new XSSFWorkbook();

                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                ISheet sheet = workbook.CreateSheet(Sheet);

                POIXMLProperties props = workbook.GetProperties();
                props.CoreProperties.Creator = "Hercules";
                props.CoreProperties.Created = DateTime.Now;

                ExportToSheet(table, sheet);

                using var file = new FileStream(fileName, FileMode.Create);
                workbook.Write(file);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = currentCulture;
            }
        }

        private void ExportToSheet(IExportTable table, ISheet sheet)
        {
            IRow header = sheet.CreateRow(0);
            int rowIndex = 1;

            var multilineStyle = sheet.Workbook.CreateCellStyle();
            multilineStyle.WrapText = true;

            IDataFormat dataFormatCustom = sheet.Workbook.CreateDataFormat();
            var dateTimeStyle = sheet.Workbook.CreateCellStyle();
            dateTimeStyle.DataFormat = dataFormatCustom.GetFormat(DateTimeFormat);

            for (int i = 0; i < table.ExportColumns.Count; i++)
            {
                header.CreateCell(i).SetCellValue(table.ExportColumns[i].Name);
            }

            for (var rowNum = 0; rowNum < table.RowCount; rowNum++)
            {
                IRow row = sheet.CreateRow(rowIndex);
                rowIndex++;
                int i = 0;
                foreach (var col in table.ExportColumns)
                {
                    var value = table.GetExportValue(rowNum, col);
                    if (value != null)
                    {
                        var cell = row.CreateCell(i);
                        switch (value)
                        {
                            case string stringValue:
                                cell.SetCellValue(stringValue);
                                if (stringValue.Contains("\n", StringComparison.Ordinal))
                                    cell.CellStyle = multilineStyle;
                                break;
                            case double doubleValue:
                                cell.SetCellValue(doubleValue);
                                break;
                            case float floatValue:
                                cell.SetCellValue(floatValue);
                                break;
                            case int intValue:
                                cell.SetCellValue(intValue);
                                break;
                            case bool boolValue:
                                cell.SetCellValue(boolValue);
                                break;
                            case DateTime dateTimeValue:
                                cell.SetCellValue(TimeZoneInfo.ConvertTimeFromUtc(dateTimeValue, TimeZone));
                                cell.CellStyle = dateTimeStyle;
                                break;
                            default:
                                cell.SetCellValue(value.ToString()!.Replace(Environment.NewLine, "\n", StringComparison.Ordinal).Replace("\n", Environment.NewLine, StringComparison.Ordinal));
                                break;
                        }
                    }
                    i++;
                }
            }
            var row1 = sheet.CreateRow(rowIndex);
            row1.HeightInPoints = 15;

            for (int i = 0; i < table.ExportColumns.Count; i++)
                sheet.AutoSizeColumn(i);
        }
    }
}
