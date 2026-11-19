using System;
using System.Collections.Immutable;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CouchDB
{
    public class CdbServer
    {
        public HttpClient HttpClient { get; }

        public CdbServer(Uri url)
            : this(url, null, null)
        {
        }

        public CdbServer(Uri url, string? user, string? password)
        {
            HttpClient = Hercules.HttpClientFactory.Create(url, user, password);
        }

        public CdbServer(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        public CdbDatabase GetDatabase(string name)
        {
            return new(this, name);
        }

        public Task<ImmutableArray<string>> GetDatabaseNamesAsync(CancellationToken cancellationToken = default)
        {
            return HttpClient.GetFromJsonAsync<ImmutableArray<string>>("_all_dbs", cancellationToken);
        }

        public async Task CreateDatabaseAsync(string dbname, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dbname);
            var response = await HttpClient.PutAsync(dbname, null!, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var result = (await response.Content.GetJsonResponseAsync(cancellationToken).ConfigureAwait(false)).AsObject;
            if (result.ContainsKey("ok") == false || result["ok"].AsBool != true)
                throw new ArgumentException("Failed to create database: " + result);
        }

        public async Task DeleteDatabaseAsync(string dbname, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dbname);
            var response = await HttpClient.DeleteAsync(dbname, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var result = (await response.Content.GetJsonResponseAsync(cancellationToken).ConfigureAwait(false)).AsObject;
            if (result.ContainsKey("ok") == false || result["ok"].AsBool != true)
                throw new ArgumentException("Failed to delete database: " + result);
        }
    }
}
