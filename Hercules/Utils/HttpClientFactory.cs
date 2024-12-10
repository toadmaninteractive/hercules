using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Hercules
{
    public static class HttpClientFactory
    {
        private static readonly SocketsHttpHandler HandlerInstance = new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(3) };

        public static HttpClient Create()
        {
            return new HttpClient(HandlerInstance, disposeHandler: false);
        }

        public static HttpClient Create(Uri? baseUri) => Create(baseUri, null, null);

        public static HttpClient Create(Uri? baseUri, string? username, string? password)
        {
            var httpClient = Create();
            if (username != null)
            {
                var basicAuthBytes = System.Text.Encoding.UTF8.GetBytes($"{username}:{password}");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(basicAuthBytes));
            }
            httpClient.BaseAddress = baseUri;
            return httpClient;
        }
    }
}
