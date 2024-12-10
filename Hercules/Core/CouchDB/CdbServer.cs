using CouchDB.Api;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Text;
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

        public async Task<CdbInstanceInfo> GetInstanceInfoAsync(CancellationToken cancellationToken = default)
        {
            var response = await HttpClient.GetAsync("", cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.GetJsonResponseAsync(CdbInstanceInfoJsonSerializer.Instance, cancellationToken).ConfigureAwait(false);
        }

        public CdbDatabase GetDatabase(string name)
        {
            return new(this, name);
        }

        public async Task<IReadOnlyCollection<string>> GetDatabaseNamesAsync(CancellationToken cancellationToken = default)
        {
            var response = await HttpClient.GetAsync("_all_dbs", cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.GetJsonResponseAsync(cancellationToken).ConfigureAwait(false);
            return result.AsArray.Select(db => db.AsString).ToArray();
        }

        public async Task CreateDatabaseAsync(string dbname, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(nameof(dbname));
            var response = await HttpClient.PutAsync(dbname, null!, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var result = (await response.Content.GetJsonResponseAsync(cancellationToken).ConfigureAwait(false)).AsObject;
            if (result.ContainsKey("ok") == false || result["ok"].AsBool != true)
                throw new ArgumentException("Failed to create database: " + result);
        }

        public async Task DeleteDatabaseAsync(string dbname, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(nameof(dbname));
            var response = await HttpClient.DeleteAsync(dbname, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var result = (await response.Content.GetJsonResponseAsync(cancellationToken).ConfigureAwait(false)).AsObject;
            if (result.ContainsKey("ok") == false || result["ok"].AsBool != true)
                throw new ArgumentException("Failed to delete database: " + result);
        }

        public async Task<CdbReplicationResult> ReplicateAsync(Uri source, Uri target, bool createTarget = false, bool continuous = false, Dictionary<string, string>? sourceHeaders = null, Dictionary<string, string>? targetHeaders = null, CancellationToken cancellationToken = default)
        {
            var sourceDatabase = new CdbReplicationDatabase(source, sourceHeaders ?? (IReadOnlyDictionary<string, string>)ImmutableDictionary<string, string>.Empty);
            var targetDatabase = new CdbReplicationDatabase(target, targetHeaders ?? (IReadOnlyDictionary<string, string>)ImmutableDictionary<string, string>.Empty);
            var request = new CdbReplicationRequest(sourceDatabase, targetDatabase, createTarget: createTarget, continuous: continuous);
            using var requestContent = new StringContent(CdbReplicationRequestJsonSerializer.Instance.Serialize(request).ToString(), Encoding.UTF8, "application/json");
            using var response = await HttpClient.PostAsync("_replicate", requestContent, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.GetJsonResponseAsync(CdbReplicationResultJsonSerializer.Instance, cancellationToken).ConfigureAwait(false);
        }
    }
}
