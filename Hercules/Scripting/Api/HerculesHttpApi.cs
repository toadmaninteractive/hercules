using Hercules.Scripting.JavaScript;
using Jint.Native;
using Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hercules.Scripting
{
    public class HerculesHttpApi
    {
        public static readonly CompletionData[] Completion = ScriptingApiHelper.GetCompletionData(typeof(HerculesHttpApi));
        private static readonly HttpClient httpClient = HttpClientFactory.Create();

        public Dictionary<string, object> Api => ScriptingApiHelper.GetApi(this);

        public ScriptContext Context { get; }
        public JsHost Host { get; }

        public HerculesHttpApi(ScriptContext context, JsHost host)
        {
            Context = context;
            Host = host;
        }

        [ScriptingApi("request", "HTTP Request.",
            Example = "hercules.http.jsonRequest('get', 'https://apiurl');")]
        public JsValue Request(string method, string url, JsValue? content, JsValue? options)
        {
            ImmutableJsonObject jsonOptions = options == null ? ImmutableJsonObject.Empty : Host.JsValueToJson(options).AsObject;
            var contentType = jsonOptions.GetValueOrDefault("contentType")?.AsString ?? "application/json";
            var jsonContent = content == null ? null : Host.JsValueToJson(content);
            var contentString = jsonContent switch
            {
                null => null,
                ImmutableJsonString s => s.AsString,
                ImmutableJson j => j.ToString(JsonFormat.Compact)
            };
            var task = RequestAsync(method, url, contentString, contentType, jsonOptions, default);
            return task.Result;
        }

        [ScriptingApi("get", "HTTP Request.",
            Example = "hercules.http.get('https://apiurl');")]
        public JsValue Get(string url, JsValue? options)
        {
            return Request("GET", url, null, options);
        }

        [ScriptingApi("post", "HTTP Request.",
            Example = "hercules.http.post('https://apiurl', content);")]
        public JsValue Post(string url, JsValue content, JsValue? options)
        {
            return Request("POST", url, content, options);
        }

        [ScriptingApi("put", "HTTP Request.",
            Example = "hercules.http.put('https://apiurl', content);")]
        public JsValue Put(string url, JsValue content, JsValue? options)
        {
            return Request("PUT", url, content, options);
        }

        private async Task<JsValue> RequestAsync(string method, string url, string? content, string contentType, ImmutableJsonObject options, CancellationToken ct)
        {
            using var httpRequest = new HttpRequestMessage(new HttpMethod(method), url);
            var apiKey = options.GetValueOrDefault("apiKey")?.AsString;
            if (apiKey != null)
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Api-Key", apiKey);
            var username = options.GetValueOrDefault("username")?.AsString;
            var password = options.GetValueOrDefault("password")?.AsString;
            if (username != null && password != null)
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));
            if (content != null)
                httpRequest.Content = new StringContent(content, Encoding.UTF8, contentType);
            using var httpResponse = await httpClient.SendAsync(httpRequest, ct).ConfigureAwait(true);
            // httpResponse.EnsureSuccessStatusCode();
            var result = await httpResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(true);
            if (httpResponse.Content.Headers.ContentType?.MediaType == "application/json")
            {
                return Host.JsonToJsValue(JsonParser.Parse(result));
            }
            return result;
        }
    }
}
