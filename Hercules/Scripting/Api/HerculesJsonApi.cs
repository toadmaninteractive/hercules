using Hercules.AI;
using Hercules.Scripting.JavaScript;
using Jint.Native;
using Json;
using JsonDiff;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hercules.Scripting
{
    public class HerculesJsonApi
    {
        public static readonly CompletionData[] Completion = ScriptingApiHelper.GetCompletionData(typeof(HerculesJsonApi));

        public Dictionary<string, object> Api => ScriptingApiHelper.GetApi(this);

        public ScriptContext Context { get; }
        public JsHost Host { get; }

        public HerculesJsonApi(ScriptContext context, JsHost host)
        {
            Context = context;
            Host = host;
        }

        [ScriptingApi("patch", "Patch JSON.",
            Example = "hercules.json.patch(json, path, value);")]
        public JsValue Patch(JsValue json, string path, JsValue value)
        {
            var source = Host.JsValueToJson(json);
            var update = Host.JsValueToJson(value);
            var p = JsonPath.Parse(path);
            var result = source.ForceUpdate(p, update);
            return Host.JsonToJsValue(result);
        }

        [ScriptingApi("fetch", "Fetch JSON value by JSON path.",
            Example = "hercules.json.fetch(json, path);")]
        public JsValue? Fetch(JsValue json, string path)
        {
            var source = Host.JsValueToJson(json);
            var p = JsonPath.Parse(path);
            if (source.TryFetch(p, out var result))
                return Host.JsonToJsValue(result);
            else
                return null;
        }

        [ScriptingApi("jq", "Run a jq program (jq 1.7 syntax) over JSON; returns an array of all results. Fails if the query is invalid, too slow or the output is too big.",
            Example = "hercules.json.jq(json, '.items[] | select(.mass > 10) | .name');")]
        public JsValue Jq(JsValue json, string query)
        {
            var inputJson = Host.JsValueToJson(json).ToString();
            var (lines, truncated) = JqRunner.RunLines(query, inputJson, TimeSpan.FromSeconds(10), 512L * 1024 * 1024);
            if (truncated)
                throw new InvalidOperationException("The jq result is too big. Make the query more selective.");
            var results = new JsonArray(lines.Select(line => JsonParser.Parse(line)));
            return Host.JsonToJsValue(results);
        }

        [ScriptingApi("diff", "JSON diff.",
            Example = "hercules.json.diff(doc1, doc2, ['_id', 'hercules_metadata'])")]
        public JsValue Diff(JsValue json1, JsValue json2, JsValue? jsExcludeKeys = null)
        {
            IEnumerable<string> excludeKeys = Array.Empty<string>();
            if (jsExcludeKeys != null)
            {
                var excludeKeysJson = Host.JsValueToJson(jsExcludeKeys);
                if (excludeKeysJson.IsArray)
                {
                    excludeKeys = excludeKeysJson.AsArray.Where(c => c.IsString).Select(c => c.AsString).ToList();
                }
            }

            var j1 = Host.JsValueToJson(json1);
            var j2 = Host.JsValueToJson(json2);
            var diff = new JsonDiffEngine().Process(j1, j2, excludeKeys);
            var chunks = diff.GetChunks();
            if (!chunks.Any())
                return JsValue.Undefined;
            var diffJson = new JsonArray(chunks.Select(c => new JsonObject { ["path"] = c.Path.ToString(), ["value1"] = c.Left ?? ImmutableJson.Null, ["value2"] = c.Right ?? ImmutableJson.Null }.ToImmutable()));
            return Host.JsonToJsValue(diffJson);
        }
    }
}
