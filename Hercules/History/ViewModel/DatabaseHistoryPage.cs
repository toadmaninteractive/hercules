using Hercules.DB;
using Hercules.Shell;
using Json;
using JsonDiff;
using NPOI;
using NPOI.HPSF;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Input;

namespace Hercules.History
{
    public class DatabaseHistoryPage : Page
    {
        public ObservableCollection<DocumentCommit> Revisions { get; } = new();
        public ObservableCollection<DocumentHistorySummary> Summary { get; } = new();
        public DatabaseHistory History { get; }

        public DateTime Since
        {
            get => field;
            set => SetField(ref field, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand ExportTableCommand { get; }

        public bool SummaryMode
        {
            get => field;
            set => SetField(ref field, value);
        }

        IDisposable? listener;

        public IReadOnlyObservableValue<bool> OpenSpreadsheetAfterExport { get; }

        public DatabaseHistoryPage(DatabaseHistory databaseHistory, IReadOnlyObservableValue<bool> openSpreadsheetAfterExport)
        {
            this.Title = "Database History";
            this.ContentId = "{DatabaseHistory}";
            this.History = databaseHistory;
            this.OpenSpreadsheetAfterExport = openSpreadsheetAfterExport;
            this.Since = DateTime.Now - new TimeSpan(1, 0, 0, 0);
            this.RefreshCommand = Commands.Execute(Refresh);
            this.ExportTableCommand = Commands.Execute(ExportTable);
            Refresh();
        }

        void Refresh()
        {
            listener?.Dispose();
            Revisions.Clear();
            Summary.Clear();
            listener = History.GetHistory(Since).Subscribe(Revisions.Add, OnComplete);
        }

        private void OnComplete()
        {
            foreach (var g in Revisions.GroupBy(rev => rev.DocumentId).OrderBy(g => g.Key))
            {
                var documentId = g.Key;
                var revs = g.OrderByDescending(r => r.RevisionNumber);
                var current = revs.First().Snapshot;
                if (current == null)
                    continue; // is that possible?
                var previous = revs.Last().PreviousSnapshot;
                var changeType = previous == null ? DocumentCommitType.Added : DocumentCommitType.Modified;
                var docSummary = new DocumentHistorySummary(documentId, changeType, current!, previous);
                Summary.Add(docSummary);
            }
        }

        protected override void OnClose()
        {
            listener?.Dispose();
        }

        void ExportTable()
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = "History",
                DefaultExt = ".xls",
                Filter = "Excel 2007 Worksheet (*.xlsx)|*.xlsx|Excel 2003 Worksheet (*.xls)|*.xls",
                Title = "Export History"
            };

            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                if (dlg.FilterIndex == 1)
                    ExportToExcel2007(dlg.FileName);
                else
                    ExportToExcel2003(dlg.FileName);
                if (OpenSpreadsheetAfterExport.Value)
                    Process.Start(new ProcessStartInfo { FileName = dlg.FileName, UseShellExecute = true });
            }
        }

        void ExportToExcel2003(string fileName)
        {
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

                ISheet historySheet = workbook.CreateSheet("History");
                ISheet summarySheet = workbook.CreateSheet("Summary");
                ExportToSheet(historySheet);
                ExportSummaryToSheet(summarySheet);

                using var file = new FileStream(fileName, FileMode.Create);
                workbook.Write(file);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = currentCulture;
            }
        }

        void ExportToExcel2007(string fileName)
        {
            var currentCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                var workbook = new XSSFWorkbook();

                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                ISheet historySheet = workbook.CreateSheet("History");
                ISheet summarySheet = workbook.CreateSheet("Summary");

                POIXMLProperties props = workbook.GetProperties();
                props.CoreProperties.Creator = "Hercules";
                props.CoreProperties.Created = DateTime.Now;

                ExportToSheet(historySheet);
                ExportSummaryToSheet(summarySheet);

                using var file = new FileStream(fileName, FileMode.Create);
                workbook.Write(file);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = currentCulture;
            }
        }

        private const int MaxCellChars = 32767;

        void ExportToSheet(ISheet sheet)
        {
            IRow header = sheet.CreateRow(0);
            int rowIndex = 1;

            var multilineStyle = sheet.Workbook.CreateCellStyle();
            multilineStyle.WrapText = true;

            IDataFormat dataFormatCustom = sheet.Workbook.CreateDataFormat();
            var dateTimeStyle = sheet.Workbook.CreateCellStyle();
            dateTimeStyle.DataFormat = dataFormatCustom.GetFormat("yyyy-MM-dd HH:mm");

            header.CreateCell(0).SetCellValue("Change");
            header.CreateCell(1).SetCellValue("Document");
            header.CreateCell(2).SetCellValue("Revision");
            header.CreateCell(3).SetCellValue("Time");
            header.CreateCell(4).SetCellValue("User");
            header.CreateCell(5).SetCellValue("Diff");

            foreach (var rev in Revisions.OrderByDescending(rev => rev.Time))
            {
                IRow row = sheet.CreateRow(rowIndex);
                rowIndex++;

                row.CreateCell(0).SetCellValue(rev.ChangeType.ToString());
                row.CreateCell(1).SetCellValue(rev.DocumentId);
                row.CreateCell(2).SetCellValue(rev.Revision);
                if (rev.Time.HasValue)
                {
                    var timeCell = row.CreateCell(3);
                    timeCell.SetCellValue(rev.Time.Value);
                    timeCell.CellStyle = dateTimeStyle;
                }
                row.CreateCell(4).SetCellValue(rev.User);
                if (rev.Changes != null)
                {
                    var diffCell = row.CreateCell(5);
                    diffCell.CellStyle = multilineStyle;
                    diffCell.SetCellValue(rev.Changes.Text.Truncate(MaxCellChars));
                }
            }

            for (int i = 0; i < 6; i++)
                sheet.AutoSizeColumn(i);
        }

        void ExportSummaryToSheet(ISheet sheet)
        {
            IRow header = sheet.CreateRow(0);
            int rowIndex = 1;

            var multilineStyle = sheet.Workbook.CreateCellStyle();
            multilineStyle.WrapText = true;

            var altMultilineStyle = sheet.Workbook.CreateCellStyle();
            altMultilineStyle.WrapText = true;
            altMultilineStyle.FillForegroundColor = IndexedColors.Grey25Percent.Index;
            altMultilineStyle.FillPattern = FillPattern.SolidForeground;

            var rowStyle = sheet.Workbook.CreateCellStyle();
            var altRowStyle = sheet.Workbook.CreateCellStyle();
            altRowStyle.FillForegroundColor = IndexedColors.Grey25Percent.Index;
            altRowStyle.FillPattern = FillPattern.SolidForeground;

            header.CreateCell(0).SetCellValue("Change");
            header.CreateCell(1).SetCellValue("Document");
            header.CreateCell(2).SetCellValue("Path");
            header.CreateCell(3).SetCellValue("Before");
            header.CreateCell(4).SetCellValue("After");

            bool isAltRow = false;
            foreach (var docSum in Summary)
            {
                if (docSum.Previous == null)
                {
                    IRow row = sheet.CreateRow(rowIndex);
                    rowIndex++;

                    var changeCell = row.CreateCell(0);
                    changeCell.SetCellValue(docSum.ChangeType.ToString());
                    changeCell.CellStyle = isAltRow ? altRowStyle : rowStyle;
                    var docCell = row.CreateCell(1);
                    docCell.SetCellValue(docSum.DocumentId);
                    docCell.CellStyle = isAltRow ? altRowStyle : rowStyle;
                    var pathCell = row.CreateCell(2);
                    pathCell.CellStyle = isAltRow ? altRowStyle : rowStyle;
                    var beforeCell = row.CreateCell(3);
                    var afterCell = row.CreateCell(4);
                    beforeCell.CellStyle = isAltRow ? altMultilineStyle : multilineStyle;
                    afterCell.CellStyle = isAltRow ? altMultilineStyle : multilineStyle;
                    afterCell.SetCellValue(docSum.Current.Json.ToString(JsonFormat.Multiline).Truncate(MaxCellChars));
                }
                else
                {
                    var diff = new JsonDiffEngine().Process(docSum.Previous.Json, docSum.Current.Json, DocumentCommitChanges.IgnoredKeys);
                    foreach (var chunk in diff.GetChunks())
                    {
                        IRow row = sheet.CreateRow(rowIndex);
                        rowIndex++;
                        var changeCell = row.CreateCell(0);
                        changeCell.SetCellValue(docSum.ChangeType.ToString());
                        changeCell.CellStyle = isAltRow ? altRowStyle : rowStyle;
                        var docCell = row.CreateCell(1);
                        docCell.SetCellValue(docSum.DocumentId);
                        docCell.CellStyle = isAltRow ? altRowStyle : rowStyle;
                        var pathCell = row.CreateCell(2);
                        pathCell.CellStyle = isAltRow ? altRowStyle : rowStyle;
                        pathCell.SetCellValue(chunk.Path.ToString());
                        var beforeCell = row.CreateCell(3);
                        var afterCell = row.CreateCell(4);
                        beforeCell.CellStyle = isAltRow ? altMultilineStyle : multilineStyle;
                        afterCell.CellStyle = isAltRow ? altMultilineStyle : multilineStyle;
                        beforeCell.SetCellValue(chunk.Left?.ToString(JsonFormat.Multiline).Truncate(MaxCellChars) ?? "");
                        afterCell.SetCellValue(chunk.Right?.ToString(JsonFormat.Multiline).Truncate(MaxCellChars) ?? "");
                    }
                }
                isAltRow = !isAltRow;
            }

            for (int i = 0; i < 5; i++)
                sheet.AutoSizeColumn(i);
        }
    }
}
