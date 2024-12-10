using System;
using Hercules.Connections;
using Hercules.Documents.Editor;
using Hercules.Forms;
using Hercules.Scripting.JavaScript;
using Hercules.Shell;
using Jint.Native;
using Json;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace Hercules.Scripting
{
    public class HerculesJsApi
    {
        public static readonly CompletionData[] Completion = ScriptingApiHelper.GetCompletionData(typeof(HerculesJsApi));

        public Dictionary<string, object> Api => ScriptingApiHelper.GetApi(this);

        public ScriptContext Context { get; }
        public JsHost Host { get; }

        public HerculesDbApi DbApi { get; }
        public HerculesProjectApi ProjectApi { get; }
        public HerculesIOApi IOApi { get; }
        public HerculesXmlApi XmlApi { get; }
        public HerculesJsonApi JsonApi { get; }
        public HerculesHttpApi HttpApi { get; }
        public HerculesGoogleApi GoogleApi { get; }

        public HerculesJsApi(ScriptContext context, JsHost host)
        {
            Context = context;
            Host = host;
            DbApi = new HerculesDbApi(context, context.ActiveDatabaseContext, host);
            ProjectApi = new HerculesProjectApi(context, host);
            IOApi = new HerculesIOApi(context, host);
            XmlApi = new HerculesXmlApi(context, host);
            JsonApi = new HerculesJsonApi(context, host);
            HttpApi = new HerculesHttpApi(context, host);
            GoogleApi = new HerculesGoogleApi(context, host);
        }

        [ScriptingApi("db", "Database API.")]
        public Dictionary<string, object> Db => DbApi.Api;

        [ScriptingApi("project", "Project API.")]
        public Dictionary<string, object> Project => ProjectApi.Api;

        [ScriptingApi("io", "IO API.")]
        public Dictionary<string, object> IO => IOApi.Api;

        [ScriptingApi("xml", "XML API.")]
        public Dictionary<string, object> XML => XmlApi.Api;

        [ScriptingApi("json", "JSON API.")]
        public Dictionary<string, object> Json => JsonApi.Api;

        [ScriptingApi("http", "HTTP API.")]
        public Dictionary<string, object> HTTP => HttpApi.Api;

        [ScriptingApi("gapi", "Google API.")]
        public Dictionary<string, object> Google => GoogleApi.Api;

        [ScriptingApi("connections", "Connections")]
        public JsValue Connections => Host.JsonToJsValue(new JsonArray(Context.Core.GetModule<ConnectionsModule>().Connections.Items.Select(ConnectionToJson)));

        [ScriptingApi("isBatchMode", "Is running in batch mode?")]
        public bool IsBatchMode => Context.Core.IsBatch;

        public static ImmutableJson ConnectionToJson(DbConnection connection)
        {
            return new JsonObject
            {
                ["title"] = connection.Title,
                ["database"] = connection.Database,
                ["url"] = connection.Url.ToString().EnsureTrailingSlash(),
                ["username"] = connection.Username
            };
        }

        [ScriptingApi("log", "Log string.",
            Example = "hercules.log(\"Hello world\");")]
        public void Log(JsValue text)
        {
            var json = Host.JsValueToJson(text);
            Context.Log(json.IsString ? json.AsString : json.ToString());
        }

        [ScriptingApi("error", "Log error message.",
            Example = "hercules.error(\"Error message\");")]
        public void Error(JsValue text)
        {
            var json = Host.JsValueToJson(text);
            Context.Error(json.IsString ? json.AsString : json.ToString());
        }

        [ScriptingApi("warning", "Log warning message.",
            Example = "hercules.warning(\"Warning message\");")]
        public void Warning(JsValue text)
        {
            var json = Host.JsValueToJson(text);
            Context.Warning(json.IsString ? json.AsString : json.ToString());
        }

        [ScriptingApi("debug", "Log debug message.",
            Example = "hercules.debug(\"Debug message\");")]
        public void Debug(JsValue text)
        {
            var json = Host.JsValueToJson(text);
            Context.Debug(json.IsString ? json.AsString : json.ToString());
        }

        [ScriptingApi("alert", "Shows a modal message box with a single OK button.",
            Example = "hercules.alert(\"Hello world\");")]
        public void Alert(string text)
        {
            Context.Core.Workspace.Scheduler.ScheduleForegroundJob(() =>
                Context.Core.Workspace.DialogService.ShowMessageBox(text, "Script", DialogButtons.Ok, DialogButtons.Ok, DialogIcon.Information));
        }

        [ScriptingApi("confirm", "Shows a modal message box with OK and Cancel button.",
            Example = "if (hercules.prompt(\"Are you sure?\")) { ... }")]
        public bool Confirm(string text)
        {
            return Context.Core.Workspace.Scheduler.ScheduleForegroundJob(() =>
                Context.Core.Workspace.DialogService.ShowMessageBox(text, "Script", DialogButtons.Ok | DialogButtons.Cancel, DialogButtons.Ok, DialogIcon.Question) == DialogButtons.Ok);
        }

        [ScriptingApi("prompt", "Shows a modal message box with a message and an edit box.",
            Example = "var name = hercules.prompt(\"Enter your name\", \"User\");")]
        public string? Prompt(string message, string? @defaultValue = null)
        {
            return Context.Core.Workspace.Scheduler.ScheduleForegroundJob(() => Context.Core.Workspace.DialogService.ShowPromptDialog(message, "Script", defaultValue));
        }

        [ScriptingApi("open", "Open document by id.",
            Example = "hercules.open(\"my_document\");")]
        public void Open(string documentId)
        {
            Context.Open(documentId);
        }

        [ScriptingApi("addSearchResult", "Add an entry to Search Results tool window.",
            Example = "hercules.addSearchResult(\"my_document\", \"path.to.field\", \"Found Text\");")]
        public void AddSearchResult(string documentId, string path, string text)
        {
            Context.EmitSearchResult(documentId, path, text);
        }

        [ScriptingApi("customDialog", "Show custom dialog",
            Example = "var valuesObject = hercules.customDialog(title, fields)")]
        public object? CustomDialog(string title, JsValue jsFields)
        {
            var jsonFields = Host.JsValueToJson(jsFields, true);
            var fields = jsonFields.IsArray ? jsonFields.AsArray.Select(f => CustomDialogField.FromJson(f.AsObject["name"].AsString, f)).ToList() :
                jsonFields.AsObject.Select(f => CustomDialogField.FromJson(f.Key, f.Value)).ToList();
            var textSizeService = new TextSizeService(new Typeface("Arial"), 11.5, Context.Core.Workspace.Dpi.PixelsPerDip);
            var dialog = new CustomDialog(title, fields, textSizeService);
            var success = Context.Core.Workspace.Scheduler.ScheduleForegroundJob(() =>
                Context.Core.Workspace.DialogService.ShowDialog(dialog));

            if (success)
                return dialog.Fields.ToDictionary(f => f.Name, f => f.GetJsValue());
            else
                return JsValue.Undefined;
        }

        [ScriptingApi("openFileDialog", "Show open file dialog. Returns selected filename or null.",
            Example = @"var filename = hercules.openFileDialog(""Load JSON"");")]
        public static string? OpenFileDialog(string title, string? fileNameOrExtension = null)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Title = title };
            if (!string.IsNullOrEmpty(fileNameOrExtension))
            {
                if (fileNameOrExtension.StartsWith('.'))
                {
                    dlg.DefaultExt = fileNameOrExtension;
                    dlg.Filter = $"*{fileNameOrExtension}|*{fileNameOrExtension}";
                }
                else
                {
                    dlg.FileName = fileNameOrExtension;
                }
            }
            if (dlg.ShowDialog() == true)
                return dlg.FileName;
            else
                return null;
        }

        [ScriptingApi("saveFileDialog", "Show save file dialog. Returns selected filename or null.",
            Example = @"var filename = hercules.saveFileDialog(""Save JSON"");")]
        public static string? SaveFileDialog(string title, string? fileNameOrExtension = null)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog { Title = title };
            if (!string.IsNullOrEmpty(fileNameOrExtension))
            {
                if (fileNameOrExtension.StartsWith('.'))
                {
                    dlg.DefaultExt = fileNameOrExtension;
                    dlg.Filter = $"*{fileNameOrExtension}|*{fileNameOrExtension}";
                }
                else
                {
                    dlg.FileName = fileNameOrExtension;
                }
            }
            if (dlg.ShowDialog() == true)
                return dlg.FileName;
            else
                return null;
        }

        [ScriptingApi("loadDatabase", "Loads database by the connection title")]
        public Dictionary<string, object> LoadDatabase(string title)
        {
            return new HerculesDbApi(Context, Context.LoadDatabase(title), Host).Api;
        }

        [ScriptingApi("activeDocId", "Returns active document id, if any, or undefined.")]
        public string? ActiveDocumentId => (Context.Core.Workspace.WindowService.ActiveContent as DocumentEditorPage)?.Document.DocumentId;

        [ScriptingApi("view", "Opens content in a separate page")]
        public void View(string title, JsValue content, JsValue? options = null)
        {
            var contentJson = Host.JsValueToJson(content);
            var optionsJson = options == null ? ImmutableJsonObject.Empty : Host.JsValueToJson(options).AsObject;
            var type = optionsJson.GetValueOrNull("type").AsStringOrNull() ?? (contentJson.IsString ? "text" : "json");
            var syntax = optionsJson.GetValueOrNull("syntax").AsStringOrNull() ??
                type switch { "text" => null, "json" => "JSON", "xml" => "XML", _ => null };
            var background = optionsJson.GetValueOrNull("background").Equals(ImmutableJson.True);
            var text = type switch
            {
                "json" => contentJson.ToString(JsonFormat.Multiline),
                _ when contentJson.IsString => contentJson.AsString,
                _ => contentJson.ToString(JsonFormat.Multiline),
            };
            if (background)
                Context.Invoke(() => Context.Core.Workspace.WindowService.AddPage(new TextPage(title, text, syntax)));
            else
                Context.Invoke(() => Context.Core.Workspace.WindowService.OpenPage(new TextPage(title, text, syntax)));
        }

        [ScriptingApi("getenv", "Returns environment variable.")]
        public string? GetEnv(string variable)
        {
            return Environment.GetEnvironmentVariable(variable);
        }
    }
}