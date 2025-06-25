using Hercules.Scripting.JavaScript;
using Jint.Native;
using Json;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Hercules.Scripting
{
    public class HerculesIOApi
    {
        public static readonly CompletionData[] Completion = ScriptingApiHelper.GetCompletionData(typeof(HerculesIOApi));

        public Dictionary<string, object> Api => ScriptingApiHelper.GetApi(this);

        public ScriptContext Context { get; }
        public JsHost Host { get; }

        public HerculesIOApi(ScriptContext context, JsHost host)
        {
            Context = context;
            Host = host;
        }

        [ScriptingApi("readDirectory", "Read list of files in the directry.",
            Example = "hercules.io.getFiles(\"D:\\\\MyProject\");")]
        public JsValue GetFiles(string path, JsValue options)
        {
            var optionsJson = options == null ? ImmutableJsonObject.Empty : Host.JsValueToJson(options).AsObject;
            var pattern = "*.*";
            if (optionsJson.TryGetValue("pattern", out var patternJson) && patternJson.IsString)
                pattern = patternJson.AsString;
            bool recursive = false;
            if (optionsJson.TryGetValue("recursive", out var recursiveJson) && recursiveJson.IsBool)
                recursive = recursiveJson.AsBool;
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var relative = false;
            if (optionsJson.TryGetValue("relative", out var relativeJson) && relativeJson.IsBool)
                relative = relativeJson.AsBool;
            var files = System.IO.Directory.GetFiles(path, pattern, searchOption);
            if (relative)
                files = files.Select(p => Path.GetRelativePath(path, p)).ToArray();
            var json = new JsonArray(files.Select(ImmutableJson.Create));
            return Host.JsonToJsValue(json);
        }

        [ScriptingApi("loadJsonFromFile", "Load JSON from file.",
            Example = "hercules.io.loadJsonFromFile(\"D:\\\\asset.json\");")]
        public JsValue LoadJsonFromFile(string fileName)
        {
            using var stream = new StreamReader(fileName);
            var json = Json.JsonParser.Parse(stream);
            return Host.JsonToJsValue(json);
        }

        [ScriptingApi("saveJsonToFile", "Save JSON to file.",
            Example = "hercules.io.saveJsonToFile(\"D:\\\\asset.json\", json);")]
        public void SaveJsonToFile(string fileName, JsValue json)
        {
            var jsonValue = Host.JsValueToJson(json);
            File.WriteAllText(fileName, jsonValue.ToString("4"));
        }

        [ScriptingApi("loadTextFromFile", "Load text from file.",
            Example = "hercules.io.loadTextFromFile(\"D:\\\\asset.txt\");")]
        public static string LoadTextFromFile(string fileName)
        {
            return File.ReadAllText(fileName);
        }

        [ScriptingApi("saveTextToFile", "Save text to file.",
            Example = "hercules.io.saveTextToFile(\"D:\\\\asset.txt\", text);")]
        public static void SaveTextToFile(string fileName, string text, string? encoding = null)
        {
            if (encoding == null)
                File.WriteAllText(fileName, text);
            else
                File.WriteAllText(fileName, text, Encoding.GetEncoding(encoding));
        }

        [ScriptingApi("loadYamlFromFile", "Load YAML from file.",
            Example = "hercules.io.loadYamlFromFile(\"D:\\\\asset.yaml\");")]
        public JsValue LoadYamlFromFile(string fileName)
        {
            var json = YamlUtils.ParseYaml(File.ReadAllText(fileName));
            return Host.JsonToJsValue(json);
        }

        [ScriptingApi("saveTableToFile", "Save table to file.",
            Example = "hercules.io.saveTableToFile(\"D:\\\\table.xlsx\", columns, rows);")]
        public void SaveTableToFile(string fileName, JsValue columns, JsValue rows, JsValue? options = null)
        {
            ImmutableJson columnsJson = Host.JsValueToJson(columns).AsArray;
            ImmutableJson rowsJson = Host.JsValueToJson(rows).AsArray;
            ImmutableJson optionsJson = options == null ? ImmutableJson.EmptyObject : Host.JsValueToJson(options);
            if (!columnsJson.IsArray)
                throw new InvalidOperationException("hercules.io.saveTableToFile expects an array as a second argument");
            if (!rowsJson.IsArray)
                throw new InvalidOperationException("hercules.io.saveTableToFile expects an array as a third argument");
            if (!optionsJson.IsObject)
                throw new InvalidOperationException("hercules.io.saveTableToFile expects an object as a fourth argument");

            var extension = Path.GetExtension(fileName).TrimStart('.');
            var sheet = optionsJson.AsObject.GetValueOrDefault("sheet", "Sheet1").AsString;
            var table = JsonExportTable.FromJson(columnsJson.AsArray, rowsJson.AsArray);
            var spreadsheetSettings = Context.Core.Workspace.SpreadsheetSettings;
            var formSettings = Context.Core.GetModule<Documents.DocumentsModule>().FormSettings;

            ITableExporter exporter = extension switch
            {
                "xls" => new ExcelExporter(ExcelFormat.Excel2003, sheet, formSettings.TimeZone.Value, spreadsheetSettings.ExportDateTimeFormat.Value),
                "xlsx" => new ExcelExporter(ExcelFormat.Excel2007, sheet, formSettings.TimeZone.Value, spreadsheetSettings.ExportDateTimeFormat.Value),
                "csv" => new CsvExporter(spreadsheetSettings.ExportCsvDelimiter.Value, formSettings.TimeZone.Value, spreadsheetSettings.ExportDateTimeFormat.Value),
                _ => new CsvExporter(spreadsheetSettings.ExportCsvDelimiter.Value, formSettings.TimeZone.Value, spreadsheetSettings.ExportDateTimeFormat.Value)
            };

            exporter.Export(table, fileName);

            if (ImmutableJson.True.Equals(optionsJson.AsObject.GetValueOrDefault("open")))
            {
                Process.Start(new ProcessStartInfo { FileName = fileName, UseShellExecute = true });
            }
        }

        [ScriptingApi("loadTableFromFile", "Load table from file.",
            Example = "hercules.io.loadTableFromFile(\"D:\\\\table.xlsx\", {sheet: 0});")]
        public JsValue LoadTableFromFile(string fileName, JsValue? options = null)
        {
            ImmutableJson optionsJson = options == null ? ImmutableJson.EmptyObject : Host.JsValueToJson(options);
            if (!optionsJson.IsObject)
                throw new InvalidOperationException("hercules.io.loadTableFromFile expects an object as a second argument");

            var extension = Path.GetExtension(fileName).TrimStart('.');
            var sheet = optionsJson.AsObject.GetValueOrDefault("sheet");

            var rows = extension switch
            {
                "xls" => LoadExcelTable(fileName, sheet),
                "xlsx" => LoadExcelTable(fileName, sheet),
                _ => LoadCsvTable(fileName)
            };

            return Host.JsonToJsValue(rows);
        }

        [ScriptingApi("getSheetNames", "Get list of workbook sheet names",
            Example = "hercules.io.getSheetNames(\"D:\\\\table.xlsx\");")]
        public object GetSheetNames(string fileName)
        {
            using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            IWorkbook workbook = WorkbookFactory.Create(stream);
            return Enumerable.Range(0, workbook.NumberOfSheets).Select(workbook.GetSheetName).ToArray();
        }

        JsonArray LoadExcelTable(string fileName, ImmutableJson? jsonSheet)
        {
            var result = new JsonArray();

            using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            IWorkbook workbook = WorkbookFactory.Create(stream);
            ISheet sheet = jsonSheet switch
            {
                ImmutableJsonInteger intSheet => workbook.GetSheetAt(intSheet.Value),
                ImmutableJsonString stringSheet => workbook.GetSheet(stringSheet.AsString),
                _ => workbook.GetSheetAt(0)
            };

            var header = sheet.GetRow(sheet.FirstRowNum);
            if (header == null)
            {
                return result;
            }
            var columns = new List<(int index, string name)>();

            for (var colIndex = header.FirstCellNum; colIndex < header.LastCellNum; colIndex++)
            {
                var cell = header.GetCell(colIndex);
                if (cell is { CellType: CellType.String, StringCellValue: var stringCellValue } && !string.IsNullOrWhiteSpace(stringCellValue))
                    columns.Add((colIndex, stringCellValue));
            }

            for (int j = sheet.FirstRowNum + 1; j <= sheet.LastRowNum; j++)
            {
                var row = sheet.GetRow(j);
                var json = new JsonObject();

                foreach (var col in columns)
                {
                    var cell = row.GetCell(col.index);
                    if (cell != null)
                    {
                        var cellType = cell.CellType;
                        if (cellType == CellType.Formula)
                            cellType = cell.CachedFormulaResultType;
                        switch (cellType)
                        {
                            case CellType.String:
                                json[col.name] = cell.StringCellValue;
                                break;

                            case CellType.Boolean:
                                json[col.name] = cell.BooleanCellValue;
                                break;

                            case CellType.Numeric:
                                json[col.name] = cell.NumericCellValue;
                                break;
                        }
                    }
                }

                result.Add(json);
            }

            return result;
        }

        JsonArray LoadCsvTable(string fileName)
        {
            var rows = CsvUtils.LoadFromFile(fileName);
            IReadOnlyList<string>? header = null;
            var result = new JsonArray();
            foreach (var row in rows)
            {
                if (header == null)
                    header = row;
                else
                {
                    var json = new JsonObject();
                    for (int i = 0; i < header.Count; i++)
                    {
                        if (i >= row.Count)
                            continue;
                        var key = header[i];
                        if (string.IsNullOrWhiteSpace(key))
                            continue;
                        json[key] = row[i];
                    }
                    result.Add(json);
                }
            }
            return result;
        }
    }

    public record ExportColumn(int Index, string Name, string? Type) : IExportColumn;

    public record JsonExportTable(IReadOnlyList<IExportColumn> ExportColumns, IReadOnlyList<ImmutableJson> Rows) : IExportTable
    {
        public static IExportTable FromJson(ImmutableJsonArray columns, ImmutableJsonArray rows)
        {
            var exportColumns = columns.AsArray.Select(JsonToExportColumn).ToList();
            return new JsonExportTable(exportColumns, rows);
        }

        static IExportColumn JsonToExportColumn(ImmutableJson json, int index)
        {
            if (json.IsString)
            {
                return new ExportColumn(index, json.AsString, null);
            }
            else if (json.IsObject)
            {
                return new ExportColumn(index, json.AsObject["name"].AsString, json.AsObject.GetValueOrDefault("type")?.AsString);
            }
            else
                throw new InvalidOperationException("Invalid table column");
        }

        public int RowCount => Rows.Count;

        public object? GetExportValue(int rowIndex, IExportColumn column)
        {
            var row = Rows[rowIndex];
            var col = (ExportColumn)column;
            if (row.IsArray && row.AsArray.Count > col.Index)
            {
                return TranslateValue(row.AsArray[col.Index], col.Type);
            }

            if (row.IsObject)
            {
                return TranslateValue(row.AsObject.GetValueOrDefault(col.Name), col.Type);
            }

            return null;
        }

        private object? TranslateValue(ImmutableJson? json, string? type)
        {
            if (json == null)
                return null;
            if (json.IsString)
                return json.AsString;
            if (json.IsInt)
                return json.AsInt;
            if (json.IsBool)
                return json.AsBool;
            if (json.IsNumber)
                return json.AsNumber;
            else
                return json.ToString();
        }
    }
}
