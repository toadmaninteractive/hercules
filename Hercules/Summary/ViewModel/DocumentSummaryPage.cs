using Hercules.Documents;
using Hercules.Forms.Schema;
using Hercules.Shell;
using Json;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace Hercules.Summary
{
    public class DocumentSummaryPage : Page
    {
        public Project Project { get; }

        public DocumentsModule DocumentsModule { get; }

        private readonly IDialogService dialogService;
        private readonly SpreadsheetSettings spreadsheetSettings;

        public DocumentSummaryPage(Project project, DocumentsModule documentModule, IDialogService dialogService, SpreadsheetSettings spreadsheetSettings, Structure structure, Category category)
        {
            this.Project = project;
            this.DocumentsModule = documentModule;
            this.dialogService = dialogService;
            this.spreadsheetSettings = spreadsheetSettings;
            this.Title = "Document Summary";
            this.ContentId = "{DocumentSummary}";
            this.Structure = structure;
            this.Category = category;
            this.SchemafulDatabase = Project.SchemafulDatabase;
            var schemaRecord = SchemafulDatabase.Schema.DocumentRoot(Category.Name).Record;
            this.table = new SummaryTable(schemaRecord, documentModule.FormSettings, documentModule.EditDocumentCommand.Single, structure.CollectPaths());
            this.CopyAllToClipboardCommand = Commands.Execute(CopyAllToClipboard);
            this.AddColumnsCommand = Commands.Execute(AddColumns);
            this.RefreshCommand = Commands.Execute(Refresh);
            this.ExportTableCommand = Commands.Execute(ExportTable);
            this.ImportTableCommand = Commands.Execute(ImportTable);
            this.ImportClipboardCommand = Commands.Execute(ImportClipboard).If(Clipboard.ContainsText);
            this.SubmitCommand = Commands.Execute(Submit).If(() => Table.IsModified);
            Refresh();

            dirtySubscription = this.OnPropertyChanged(nameof(Table), page => page.Table)
                .Select(table => Observable.Return(table.IsModified).Concat(table.OnPropertyChanged(nameof(table.IsModified), t => t.IsModified)))
                .Switch()
                .Subscribe(isModified => IsDirty = isModified);
        }

        static string? lastFileName;

        SummaryTable table;
        readonly IDisposable dirtySubscription;

        public SummaryTable Table
        {
            get => table;
            private set => SetField(ref table, value);
        }

        public Structure Structure { get; private set; }
        public Category Category { get; private set; }
        public SchemafulDatabase SchemafulDatabase { get; private set; }

        public ICommand CopyAllToClipboardCommand { get; }
        public ICommand AddColumnsCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ExportTableCommand { get; }
        public ICommand ImportTableCommand { get; }
        public ICommand ImportClipboardCommand { get; }
        public ICommand SubmitCommand { get; }

        public event Action OnReset = delegate { };

        bool isReadOnly = false;

        public bool IsReadOnly
        {
            get => isReadOnly;
            set => SetField(ref isReadOnly, value);
        }

        public void AddColumns()
        {
            var dialog = new SummaryParamsDialog(Project.SchemafulDatabase, Category, Structure);
            if (dialogService.ShowDialog(dialog))
            {
                this.Category = dialog.Category!;
                this.SchemafulDatabase = dialog.SchemafulDatabase;
                this.Structure = dialog.Structure!;
                var schemaRecord = SchemafulDatabase.Schema.DocumentRoot(Category.Name).Record;
                this.Table = new SummaryTable(schemaRecord, DocumentsModule.FormSettings, DocumentsModule.EditDocumentCommand.Single, Structure.CollectPaths());
                Refresh();
            }
        }

        protected override void OnClose()
        {
            dirtySubscription.Dispose();
            base.OnClose();
        }

        public void Refresh()
        {
            Table.Rows.Clear();
            Table.Rows.AddRange(Category.Documents.Select(doc => new TableRow(Table, doc)));
            Table.IsModified = false;
            OnReset();
        }

        public void Submit()
        {
            foreach (var row in Table.Rows)
            {
                if (!row.IsModified)
                    continue;

                Project.SchemafulDatabase.AllDocuments.TryGetValue(row.DocumentId, out var doc);
                if (doc == null)
                {
                    JsonObject newDoc;
                    if (row.Document != null)
                    {
                        newDoc = new JsonObject(row.Document.Json);
                        newDoc.Remove("_rev");
                    }
                    else
                    {
                        newDoc = new JsonObject
                        {
                            [SchemafulDatabase.Schema.Variant.Tag] = Category.Name
                        };
                    }
                    var schemalessDoc = Project.Database.CreateDocument(row.DocumentId, new DocumentDraft(newDoc));
                    doc = Project.SchemafulDatabase.AllDocuments[row.DocumentId];
                }

                var builder = new JsonBuilder(doc.Json);
                JsonPath? firstPath = null;
                foreach (var col in Table.Columns)
                {
                    var cell = row.GetCell(col);
                    if (!col.IsId && cell.IsModified)
                    {
                        if (cell.Json != null)
                        {
                            builder.ForceUpdate(col.Path, cell.Json);
                            if (firstPath != null)
                                firstPath = col.Path;
                        }
                        else
                            builder.Delete(col.Path);
                    }
                }

                DocumentsModule.EditDocument(doc.DocumentId, builder.ToImmutable().AsObject, firstPath);
            }
        }

        public void CopyAllToClipboard()
        {
            var exporter = new CsvExporter('\t', DocumentsModule.FormSettings.TimeZone.Value, spreadsheetSettings.ExportDateTimeFormat.Value);
            Clipboard.SetText(exporter.ExportToString(Table));
        }

        public void ExportTable()
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = Category.Name + " summary",
                DefaultExt = ".xls",
                Filter = "Excel 2007 Worksheet (*.xlsx)|*.xlsx|Excel 2003 Worksheet (*.xls)|*.xls|CSV (*.csv)|*.csv",
                Title = "Export Table"
            };

            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                lastFileName = dlg.FileName;
                var timeZone = DocumentsModule.FormSettings.TimeZone.Value;

                ITableExporter exporter = dlg.FilterIndex switch
                {
                    1 => new ExcelExporter(ExcelFormat.Excel2007, Category.Name, timeZone, spreadsheetSettings.ExportDateTimeFormat.Value),
                    2 => new ExcelExporter(ExcelFormat.Excel2003, Category.Name, timeZone, spreadsheetSettings.ExportDateTimeFormat.Value),
                    _ => new CsvExporter(spreadsheetSettings.ExportCsvDelimiter.Value, timeZone, spreadsheetSettings.ExportDateTimeFormat.Value),
                };
                exporter.Export(Table, dlg.FileName);
                if (spreadsheetSettings.OpenSpreadsheetAfterExport.Value)
                    Process.Start(new ProcessStartInfo { FileName = dlg.FileName, UseShellExecute = true });
            }
        }

        public void ImportTable()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".csv",
                FilterIndex = 5,
                Filter = "Excel 2007 Worksheet (*.xlsx)|*.xlsx|Excel 2003 Worksheet (*.xls)|*.xls|CSV (*.csv)|*.csv|Text (*.txt)|*.txt|All Supported Files (*.xlsx;*.xls;*.csv;*.txt)|*.xlsx;*.xls;*.csv;*.txt|All Files (*.*)|*.*",
                Title = "Import Table",
                FileName = lastFileName
            };

            if (dlg.ShowDialog() == true)
            {
                var ext = Path.GetExtension(dlg.FileName);
                if (string.Equals(ext, ".xlsx", StringComparison.OrdinalIgnoreCase) || string.Equals(ext, ".xls", StringComparison.OrdinalIgnoreCase))
                {
                    ImportExcel(dlg.FileName!);
                }
                else
                {
                    ImportCsv(CsvUtils.LoadFromFile(dlg.FileName!));
                }
            }
        }

        void ImportClipboard()
        {
            ImportCsv(CsvUtils.LoadFromString(Clipboard.GetText()));
        }

        bool ProcessHeader(int from, int to, Func<int, string?> getter, out int idIndex, [MaybeNullWhen(false)] out Dictionary<int, TableColumn> columns, out bool allowNewDocuments, [MaybeNullWhen(false)] out TimeZoneInfo timeZone)
        {
            idIndex = -1;
            columns = null;
            allowNewDocuments = false;
            timeZone = null;
            List<ImportColumn> importColumns = new List<ImportColumn>();
            for (int i = from; i < to; i++)
            {
                var item = getter(i);
                if (string.IsNullOrWhiteSpace(item))
                    continue;
                if (item.TrimStart('_').Equals("id", StringComparison.OrdinalIgnoreCase))
                {
                    idIndex = i;
                }
                else
                {
                    var column = Table.Columns.FirstOrDefault(col => col.Name == item);
                    JsonPath? path;
                    try
                    {
                        path = JsonPath.Parse(item);
                    }
                    catch (ArgumentException)
                    {
                        path = null;
                    }
                    if (path != null)
                    {
                        bool isValid = column != null || SchemafulDatabase.Schema.DocumentRoot(Category.Name).GetByPath(path) != null;
                        if (isValid)
                        {
                            var importColumn = new ImportColumn(item, i, true, column);
                            importColumns.Add(importColumn);
                        }
                    }
                }
            }
            if (idIndex < 0)
            {
                dialogService.ShowError("Failed to import table: no _id column found");
                return false;
            }
            var dialog = new ImportColumnsDialog(importColumns, DocumentsModule.FormSettings.TimeZone.Value);
            if (dialogService.ShowDialog(dialog))
            {
                allowNewDocuments = dialog.AllowNewDocuments;
                timeZone = dialog.TimeZone;

                columns = new Dictionary<int, TableColumn>();

                bool shouldReset = false;

                foreach (var importColumn in importColumns)
                {
                    if (importColumn.IsChecked)
                    {
                        var column = importColumn.TableColumn;
                        if (column == null)
                        {
                            column = Table.AddColumn(JsonPath.Parse(importColumn.Name));
                            shouldReset = true;
                        }
                        columns[importColumn.Index] = column;
                    }
                }

                if (shouldReset)
                    OnReset();

                return true;
            }
            else
                return false;
        }

        void ImportCsv(IEnumerable<IReadOnlyList<string>> rows)
        {
            bool isHeader = true;
            Dictionary<int, TableColumn>? columns = null;
            int idIndex = -1;
            bool allowNewDocuments = false;
            TimeZoneInfo? timeZone = null;
            foreach (var row in rows)
            {
                if (isHeader)
                {
                    if (!ProcessHeader(0, row.Count, i => row[i], out idIndex, out columns, out allowNewDocuments, out timeZone))
                        return;
                    isHeader = false;
                }
                else
                {
                    var id = row[idIndex];
                    var tableRow = GetOrCreateRow(id, allowNewDocuments);
                    if (tableRow != null)
                    {
                        foreach (var pair in columns!)
                        {
                            var cell = tableRow.GetCell(pair.Value);
                            var value = row[pair.Key];
                            if (pair.Value.Type is DateTimeSchemaType && DateTimeSchemaType.ConvertFromString(row[pair.Key], timeZone!, out var dateTimeValue))
                                cell.ObjectValue = dateTimeValue;
                            else
                                cell.StringValue = value;
                        }
                    }
                }
            }
        }

        public void ImportExcel(string fileName)
        {
            using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            IWorkbook workbook = WorkbookFactory.Create(stream);
            ISheet sheet = workbook.GetSheetAt(0);
            var header = sheet.GetRow(sheet.FirstRowNum);

            string? GetCellValue(int i)
            {
                var cell = header.GetCell(i);
                if (cell is { CellType: CellType.String })
                    return cell.StringCellValue;
                else
                    return null;
            }

            if (!ProcessHeader(header.FirstCellNum, header.LastCellNum, GetCellValue, out var idIndex, out var columns, out bool allowNewDocuments, out var timeZone))
                return;

            for (int j = sheet.FirstRowNum + 1; j <= sheet.LastRowNum; j++)
            {
                var row = sheet.GetRow(j);

                var idCell = row.GetCell(idIndex);
                if (idCell != null)
                {
                    var tableRow = GetOrCreateRow(idCell.StringCellValue, allowNewDocuments);
                    if (tableRow != null)
                    {
                        foreach (var col in columns)
                        {
                            var cell = row.GetCell(col.Key);
                            if (cell != null)
                            {
                                var tableCell = tableRow.GetCell(col.Value);
                                var cellType = cell.CellType;
                                if (cellType == CellType.Formula)
                                    cellType = cell.CachedFormulaResultType;
                                switch (cellType)
                                {
                                    case CellType.String:
                                        tableCell.ObjectValue = cell.StringCellValue;
                                        break;

                                    case CellType.Boolean:
                                        tableCell.ObjectValue = cell.BooleanCellValue;
                                        break;

                                    case CellType.Blank:
                                        tableCell.ObjectValue = null;
                                        break;

                                    case CellType.Numeric:
                                        if (col.Value.Type.Type == typeof(int))
                                            tableCell.ObjectValue = (int)cell.NumericCellValue;
                                        else if (col.Value.Type.Type == typeof(DateTime))
                                            tableCell.ObjectValue = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(cell.DateCellValue.GetValueOrDefault(), DateTimeKind.Unspecified), timeZone);
                                        else
                                            tableCell.ObjectValue = cell.NumericCellValue;
                                        break;

                                    default:
                                        tableCell.ObjectValue = null;
                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private TableRow? GetOrCreateRow(string documentId, bool allowNewDocuments)
        {
            var tableRow = Table.Rows.FirstOrDefault(r => r.DocumentId == documentId);
            if (tableRow == null)
            {
                if (SchemafulDatabase.AllDocuments.TryGetValue(documentId, out var doc))
                {
                    tableRow = Table.AddRow(doc);
                }
                else if (allowNewDocuments)
                {
                    var isValid = Regex.IsMatch(documentId, @"^[a-zA-Z]{1}[a-zA-Z0-9_]*$");
                    if (isValid)
                    {
                        tableRow = Table.AddNewRow(documentId);
                        Table.IsModified = true;
                    }
                    else
                        Logger.LogWarning($"Invalid document id: {documentId}");
                }
            }
            return tableRow;
        }
    }
}
