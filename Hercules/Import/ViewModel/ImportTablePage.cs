using Hercules.Documents;
using Hercules.Scripting;
using Hercules.Scripting.JavaScript;
using Hercules.Shell;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TextDocument = ICSharpCode.AvalonEdit.Document.TextDocument;

namespace Hercules.Import
{
    public class ImportTablePage : ValidatedPage
    {
        private readonly ScriptingModule scriptingModule;

        public Project Project { get; }

        public ImportTablePage(Project project, ScriptingModule scriptingModule)
        {
            this.Project = project;
            this.scriptingModule = scriptingModule;
            this.Title = "Import Table";
            this.ContentId = "{ImportTable}";

            Job = new ForegroundJob();

            RunCommand = Commands.ExecuteAsync(RunScriptJobAsync);
            LoadFromFileCommand = Commands.Execute(LoadTable);
            PasteCommand = Commands.Execute(PasteTable);
            LoadJsonFromFileCommand = Commands.Execute(LoadJson);
            PasteJsonCommand = Commands.Execute(PasteJson);

            ScriptEditor = new TextDocument(string.Empty);
            ImportColumns = new ObservableCollection<MapTableColumn>();

            ImportColumns.CollectionChanged += (sender, e) => { if (!isUpdatingFields) GenerateScript(); };
        }

        bool isUpdatingFields;
        private readonly Regex idRegex = new(@"^[a-zA-Z]{1}[a-zA-Z0-9_]*$");

        public ICommand RunCommand { get; }
        public ICommand LoadFromFileCommand { get; }
        public ICommand PasteCommand { get; }
        public ICommand LoadJsonFromFileCommand { get; }
        public ICommand PasteJsonCommand { get; }

        string? lastFileName;
        bool isCompleted;

        public bool IsCompleted
        {
            get => isCompleted;
            set => SetField(ref isCompleted, value);
        }

        public ForegroundJob Job { get; private set; }

        public TextDocument ScriptEditor { get; private set; }

        public ObservableCollection<MapTableColumn> ImportColumns { get; private set; }

        RawTable? rawTable;

        public RawTable? RawTable
        {
            get => rawTable;
            set
            {
                if (rawTable != value)
                {
                    rawTable = value;
                    if (rawTable != null)
                    {
                        rawTable.RemoveEmptyRows();
                        MapTable = new MapTable(rawTable);
                    }
                    RaisePropertyChanged();
                }
            }
        }

        MapTable? mapTable;

        public MapTable? MapTable
        {
            get => mapTable;
            set
            {
                if (mapTable != value)
                {
                    mapTable = value;
                    RaisePropertyChanged();
                    if (mapTable != null)
                    {
                        IdColumn = mapTable.Columns.FirstOrDefault(c => IsTextSimilar(c.Title, "id"));
                        if (Category != null)
                            SuggestImportFields();
                    }
                }
            }
        }

        void SuggestImportFields()
        {
            isUpdatingFields = true;
            try
            {
                var schema = Project.SchemafulDatabase.Schema;
                var type = schema.DocumentRoot(Category.Name);
                ImportColumns.Clear();
                ImportColumns.AddRange(MapTable.Columns.Where(c => c != IdColumn && type.Record.Fields.Any(f => f.Name == c.Field)));
            }
            finally
            {
                isUpdatingFields = false;
            }
            GenerateScript();
        }

        void GenerateScript()
        {
            var sb = new StringBuilder();
            sb.AppendLine("for (var row of table) {");
            var defaultJson = Category == null ? "{}" : $"{{\"{Project.SchemafulDatabase.Schema?.Variant?.Tag}\": \"{Category.Name}\"}}";
            var id = IdColumn?.Title ?? "_id";
            sb.AppendLine($"    var doc = hercules.db.getOrCreate(row['{id}'], {defaultJson});");
            foreach (var col in ImportColumns)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "    doc.{0} = row['{0}'];\n", col.Field);
            }

            sb.AppendLine("    hercules.db.update(doc);");
            sb.AppendLine("}");
            ScriptEditor.Text = sb.ToString();
        }

        private static bool IsTextSimilar(string value, string pattern)
        {
            if (string.IsNullOrEmpty(value))
                return false;
            return value.Replace("_", string.Empty, StringComparison.Ordinal).Replace(" ", string.Empty, StringComparison.Ordinal).Equals(pattern, StringComparison.OrdinalIgnoreCase);
        }

        MapTableColumn? idColumn;

        [Required(ErrorMessage = "Select a column that contains document ids")]
        public MapTableColumn? IdColumn
        {
            get => idColumn;
            set
            {
                if (idColumn != value)
                {
                    ForeachCellInColumn(idColumn, v => { v.IsId = false; v.IsValid = true; });
                    idColumn = value;
                    ForeachCellInColumn(idColumn, v => { v.IsId = true; v.IsValid = IsValidId(v.Text); });
                    RaisePropertyChanged();
                    if (Category != null)
                        SuggestImportFields();
                }
            }
        }

        Category? category;

        [Required(ErrorMessage = "Select category")]
        public Category? Category
        {
            get => category;
            set => SetField(ref category, value);
        }

        bool IsValidId(string? id)
        {
            return id != null && idRegex.IsMatch(id);
        }

        void ForeachCellInColumn(MapTableColumn? column, Action<MapTableValue> action)
        {
            if (column == null || mapTable == null)
                return;
            foreach (var row in mapTable.Rows)
            {
                if (column.Index < row.Items.Count)
                    action(row.Items[column.Index]);
            }
        }

        void LoadTable()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog()
            {
                DefaultExt = ".csv",
                Filter = "Excel 2007 Worksheet (*.xlsx)|*.xlsx|Excel 2003 Worksheet (*.xls)|*.xls|CSV (*.csv)|*.csv|Text (*.txt)|*.txt|All Files (*.*)|*.*",
                Title = "Import Table"
            };
            if (lastFileName != null)
                dlg.FileName = lastFileName;

            if (dlg.ShowDialog() == true)
            {
                lastFileName = dlg.FileName;
                var ext = Path.GetExtension(dlg.FileName);
                if (ext.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) || ext.Equals(".xls", StringComparison.OrdinalIgnoreCase))
                {
                    RawTable = new ExcelImportTable().LoadFromFile(dlg.FileName);
                }
                else
                {
                    RawTable = CsvImportTable.LoadFromFile(dlg.FileName);
                }
            }
        }

        void PasteTable()
        {
            var data = Clipboard.GetDataObject();
            string? importText = null;
            if (data == null)
            {
                importText = null;
            }
            else if (data.GetDataPresent(DataFormats.CommaSeparatedValue, true))
            {
                importText = data.GetData(DataFormats.CommaSeparatedValue, true) as string;
            }
            else if (data.GetDataPresent(DataFormats.UnicodeText, true))
            {
                importText = data.GetData(DataFormats.UnicodeText, true) as string;
            }
            else if (Clipboard.ContainsText())
            {
                importText = Clipboard.GetText();
            }
            RawTable = CsvImportTable.LoadFromText(importText ?? string.Empty);
        }

        void LoadJson()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Import Json"
            };
            if (lastFileName != null)
                dlg.FileName = lastFileName;

            if (dlg.ShowDialog() == true)
            {
                lastFileName = dlg.FileName;
                RawTable = JsonImportTable.LoadFromFile(dlg.FileName);
            }
        }

        void PasteJson()
        {
            var data = Clipboard.GetDataObject();
            var text = string.Empty;
            if (data == null)
            {
            }
            else if (data.GetDataPresent(DataFormats.UnicodeText, true))
            {
                text = (string)data.GetData(DataFormats.UnicodeText, true);
            }
            else if (Clipboard.ContainsText())
            {
                text = Clipboard.GetText();
            }
            RawTable = JsonImportTable.LoadFromText(text);
        }

        async Task RunScriptJobAsync()
        {
            await Job.Run("Run Script", RunScriptAsync).ConfigureAwait(false);
        }

        async Task RunScriptAsync(IProgress<string> progress, CancellationToken cancellationToken)
        {
            var context = scriptingModule.CreateScriptContext();
            var script = ScriptEditor.Text;
            var success = await Task.Run(() => DoRunScript(script, MapTable, context, progress, cancellationToken), cancellationToken).ConfigureAwait(true);
            if (success)
            {
                IsCompleted = true;
                await context.FlushAsync();
            }
        }

        static bool DoRunScript(string script, MapTable table, ScriptContext context, IProgress<string> progress, CancellationToken cancellationToken)
        {
            progress.Report("Compiling...");
            var errors = JsHost.SyntaxCheck(script);
            if (errors == null)
            {
                progress.Report("Processing...");
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var host = new JsHost(context.ScriptingModuleProvider);
                    var jsonRows = table.ToJson();
                    host.SetValue("table", host.JsonToJsValue(jsonRows));
                    host.SetValue("hercules", new HerculesJsApi(context, host).Api);
                    try
                    {
                        host.Execute(script);
                    }
                    catch (Exception e)
                    {
                        Logger.LogException("Script error", e);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
                return true;
            }
            else
            {
                foreach (var ex in errors)
                    Logger.LogException("Script error", ex);
            }
            return false;
        }
    }
}
