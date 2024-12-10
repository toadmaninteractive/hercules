using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using Hercules.Scripting.JavaScript;
using Jint.Native;
using Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Hercules.Scripting
{
    public class HerculesGoogleApi
    {
        public static readonly CompletionData[] Completion = ScriptingApiHelper.GetCompletionData(typeof(HerculesGoogleApi));

        public Dictionary<string, object> Api => ScriptingApiHelper.GetApi(this);

        public ScriptContext Context { get; }
        public JsHost Host { get; }

        public HerculesGoogleApi(ScriptContext context, JsHost host)
        {
            Context = context;
            Host = host;
        }

        static readonly string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };
        private const string ApplicationName = "Toadman Hercules";

        [ScriptingApi("getSheetNames", "Get list of spreadsheet sheet names",
            Example = "hercules.gapi.getSheetNames('1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms');")]
        public object GetSheetNames(string spreadsheetId, JsValue? options = null)
        {
            ImmutableJson optionsJson = options == null ? ImmutableJson.EmptyObject : Host.JsValueToJson(options);
            if (!optionsJson.IsObject)
                throw new InvalidOperationException("hercules.gapi.loadTable expects an object as a second argument");
            var credentialJson = optionsJson.AsObject["credential"];
            var service = CreateSheetsService(credentialJson);
            var request = service.Spreadsheets.Get(spreadsheetId);
            var spreadsheet = request.Execute();
            return spreadsheet.Sheets.Select(sheet => sheet.Properties.Title).ToArray();
        }

        [ScriptingApi("loadTable", "Load table",
            Example = "hercules.gapi.loadTable('1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms'")]
        public JsValue LoadTable(string spreadsheetId, JsValue? options = null)
        {
            ImmutableJson optionsJson = options == null ? ImmutableJson.EmptyObject : Host.JsValueToJson(options);
            if (!optionsJson.IsObject)
                throw new InvalidOperationException("hercules.gapi.loadTable expects an object as a second argument");

            var credentialJson = optionsJson.AsObject["credential"];
            var service = CreateSheetsService(credentialJson);

            var sheet = optionsJson.AsObject.GetValueOrDefault("sheet").AsStringOrNull();
            var range = optionsJson.AsObject.GetValueOrDefault("range").AsStringOrNull() ?? "A:Z";
            if (!string.IsNullOrEmpty(sheet))
                range = $"'{sheet}'!{range}";

            SpreadsheetsResource.ValuesResource.GetRequest request = service.Spreadsheets.Values.Get(spreadsheetId, range);

            ValueRange response = request.Execute();
            IList<IList<object>> rows = response.Values;

            if (rows == null || rows.Count == 0)
            {
                return JsValue.Undefined;
            }

            JsonArray result = new JsonArray();

            var useHeader = optionsJson.AsObject.GetValueOrDefault("header", ImmutableJson.True).AsBool;

            if (useHeader)
            {
                var header = rows[0];
                var columns = new List<(int index, string name)>();
                for (var colIndex = 0; colIndex < header.Count; colIndex++)
                {
                    var cell = header[colIndex];
                    var cellName = cell?.ToString();
                    if (cellName != null)
                        columns.Add((colIndex, cellName));
                }

                for (int j = 1; j < rows.Count; j++)
                {
                    var row = rows[j];
                    var json = new JsonObject();

                    foreach (var col in columns)
                    {
                        if (col.index < row.Count && row[col.index] != null)
                        {
                            var cellValue = row[col.index]?.ToString();
                            json[col.name] = cellValue == null ? ImmutableJson.Null : ImmutableJson.Create(cellValue);
                        }
                    }

                    result.Add(json);
                }
            }
            else
            {
                for (int j = 0; j < rows.Count; j++)
                {
                    var row = rows[j];
                    var json = new JsonArray();

                    for (var colIndex = 0; colIndex < row.Count; colIndex++)
                    {
                        var cellValue = row[colIndex]?.ToString();
                        json.Add(cellValue == null ? ImmutableJson.Null : ImmutableJson.Create(cellValue));
                    }

                    result.Add(json);
                }
            }

            return Host.JsonToJsValue(result);
        }

        private SheetsService CreateSheetsService(ImmutableJson credentialJson)
        {
            using var credentialStream = new MemoryStream();
            using (var credentialStreamWriter = new StreamWriter(credentialStream, leaveOpen: true))
                credentialStreamWriter.Write(credentialJson.ToString());
            credentialStream.Position = 0;
            var credPath = Path.Combine(PathUtils.RootFolder, "Hercules", "GoogleClient");
            UserCredential userCredential = GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.FromStream(credentialStream).Secrets, Scopes, "user", CancellationToken.None, new FileDataStore(credPath, true)).Result;

            return new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = userCredential,
                ApplicationName = ApplicationName,
            });
        }
    }
}

