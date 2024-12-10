using CouchDB.Api;
using Json;
using Json.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CouchDB
{
    public class CdbDatabase
    {
        public CdbServer Server { get; }
        public string Name { get; }

        public CdbDatabase(CdbServer server, string name)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Database name cannot be empty", nameof(name));
            Server = server;
            Name = name;
        }

        public async Task<CdbDatabaseInfo> GetInfoAsync(CancellationToken cancellationToken = default)
        {
            using var response = await Server.HttpClient.GetAsync(Name, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.GetJsonResponseAsync(CdbDatabaseInfoJsonSerializer.Instance, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns information of all of the documents in a given database.
        /// </summary>
        /// <param name="conflicts">Includes conflicts information in response. Ignored if include_docs isn’t true. Default is false.</param>
        /// <param name="descending">Return the documents in descending by key order. Default is false.</param>
        /// <param name="endKey">Stop returning records when the specified key is reached. Optional.</param>
        /// <param name="endKeyDocId">Stop returning records when the specified document ID is reached. Optional.</param>
        /// <param name="includeDocs">Include the full content of the documents in the return. Default is false.</param>
        /// <param name="inclusiveEnd">Specifies whether the specified end key should be included in the result. Default is true.</param>
        /// <param name="key">Return only documents that match the specified key. Optional.</param>
        /// <param name="limit">Limit the number of the returned documents to the specified number. Optional.</param>
        /// <param name="skip">Skip this number of records before starting to return the results. Default is 0.</param>
        /// <param name="stale">Allow the results from a stale view to be used, without triggering a rebuild of all views within the encompassing design doc. Supported values: ok and update_after. Optional.</param>
        /// <param name="startKey">Return records starting with the specified key. Optional.</param>
        /// <param name="startKeyDocId">Return records starting with the specified document ID. Optional.</param>
        /// <param name="updateSeq">Response includes an update_seq value indicating which sequence id of the underlying database the view reflects. Default is false.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>CdbAllDocs structure</returns>
        public async Task<CdbAllDocs> GetAllDocumentsAsync(bool conflicts = false, bool descending = false, string? endKey = null, string? endKeyDocId = null, bool includeDocs = false, bool inclusiveEnd = true, string? key = null, int? limit = null, int skip = 0, string? stale = null, string? startKey = null, string? startKeyDocId = null, bool updateSeq = false, CancellationToken cancellationToken = default)
        {
            var url = new CdbQueryBuilder(Name, "_all_docs")
                .AppendBoolean("conflicts", conflicts, false)
                .AppendBoolean("descending", descending, false)
                .AppendString("endkey", endKey)
                .AppendString("endkey_docid", endKeyDocId)
                .AppendBoolean("include_docs", includeDocs, false)
                .AppendBoolean("inclusive_end", inclusiveEnd, false)
                .AppendString("key", key)
                .AppendNumber("limit", limit)
                .AppendNumber("skip", skip, 0)
                .AppendString("stale", stale)
                .AppendString("startkey", startKey)
                .AppendString("startkey_docid", startKeyDocId)
                .AppendBoolean("update_seq", updateSeq, false)
                .ToUri();
            using var response = await Server.HttpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.GetJsonResponseAsync(CdbAllDocsJsonSerializer.Instance, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns a sorted list of changes made to documents in the database, in time order of application, can be obtained from the database’s _changes resource.
        /// Only the most recent change for a given document is guaranteed to be provided, for example if a document has had fields added, and then deleted, an API client checking for changes will not necessarily receive the intermediate state of added documents.
        /// 
        /// This can be used to listen for update and modifications to the database for post processing or synchronization, and for practical purposes, a continuously connected _changes feed is a reasonable approach for generating a real-time log for most applications.
        /// </summary>
        /// <param name="docIds">List of document IDs to filter the changes feed as valid JSON array. Used with _doc_ids filter. Since length of URL is limited, it is better to use POST /{db}/_changes instead.</param>
        /// <param name="conflicts">Includes conflicts information in response. Ignored if include_docs isn’t true. Default is false.</param>
        /// <param name="descending">Return the change results in descending sequence order (most recent change first). Default is false.</param>
        /// <param name="feed">see Changes Feeds. Default is normal.</param>
        /// <param name="filter">Reference to a filter function from a design document that will filter whole stream emitting only filtered events. See the section Change Notifications in the book CouchDB The Definitive Guide for more information.</param>
        /// <param name="heartbeat">Period in milliseconds after which an empty line is sent in the results. Only applicable for longpoll or continuous feeds. Overrides any timeout to keep the feed alive indefinitely. Default is 60000. May be true to use default value.</param>
        /// <param name="includeDocs">Include the associated document with each result. If there are conflicts, only the winning revision is returned. Default is false.</param>
        /// <param name="attachments">Include the Base64-encoded content of attachments in the documents that are included if include_docs is true. Ignored if include_docs isn’t true. Default is false.</param>
        /// <param name="attEncodingInfo">Include encoding information in attachment stubs if include_docs is true and the particular attachment is compressed. Ignored if include_docs isn’t true. Default is false.</param>
        /// <param name="limit">Limit number of result rows to the specified value (note that using 0 here has the same effect as 1).</param>
        /// <param name="since">Start the results from the change immediately after the given sequence number. Can be integer number or now value. Default is 0.</param>
        /// <param name="style">Specifies how many revisions are returned in the changes array. The default, main_only, will only return the current “winning” revision; all_docs will return all leaf revisions (including conflicts and deleted former conflicts).</param>
        /// <param name="timeout">Maximum period in milliseconds to wait for a change before the response is sent, even if there are no results. Only applicable for longpoll or continuous feeds. Default value is specified by httpd/changes_timeout configuration option. Note that 60000 value is also the default maximum timeout to prevent undetected dead connections.</param>
        /// <param name="view">Allows to use view functions as filters. Documents counted as “passed” for view filter in case if map function emits at least one record for them. See _view for more info.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>CdbChanges structure</returns>
        public async Task<CdbChanges> GetChangesAsync(IEnumerable<string>? docIds = null, bool conflicts = false, bool descending = false, string? feed = null, string? filter = null, int? heartbeat = null, bool includeDocs = false, bool attachments = false, bool attEncodingInfo = false, int? limit = null, string? since = null, string? style = null, int? timeout = null, string? view = null, CancellationToken cancellationToken = default)
        {
            var url = new CdbQueryBuilder(Name, "_changes")
                .AppendBoolean("conflicts", conflicts, false)
                .AppendBoolean("descending", descending, false)
                .AppendString("feed", feed)
                .AppendString("filter", filter)
                .AppendNumber("heartbeat", heartbeat)
                .AppendBoolean("include_docs", includeDocs, false)
                .AppendBoolean("attachments", attachments, false)
                .AppendBoolean("att_encoding_info", attEncodingInfo, false)
                .AppendNumber("limit", limit)
                .AppendString("since", since)
                .AppendString("style", style)
                .AppendNumber("timeout", timeout)
                .AppendString("view", view)
                .ToUri();
            using var response = await Server.HttpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.GetJsonResponseAsync(CdbChangesJsonSerializer.Instance, cancellationToken).ConfigureAwait(false);
        }

        public async Task GetChangesContinuousAsync(Action<CdbChange> callback, IEnumerable<string>? docIds = null, bool conflicts = false, bool descending = false, string? feed = null, string? filter = null, int? heartbeat = null, bool includeDocs = false, bool attachments = false, bool attEncodingInfo = false, int? limit = null, string? since = null, string? style = null, int? timeout = null, string? view = null, CancellationToken cancellationToken = default)
        {
            var url = new CdbQueryBuilder(Name, "_changes")
                .AppendString("feed", "continuous")
                .AppendBoolean("conflicts", conflicts, false)
                .AppendBoolean("descending", descending, false)
                .AppendString("feed", feed)
                .AppendString("filter", filter)
                .AppendNumber("heartbeat", heartbeat)
                .AppendBoolean("include_docs", includeDocs, false)
                .AppendBoolean("attachments", attachments, false)
                .AppendBoolean("att_encoding_info", attEncodingInfo, false)
                .AppendNumber("limit", limit)
                .AppendString("since", since)
                .AppendString("style", style)
                .AppendNumber("timeout", timeout)
                .AppendString("view", view)
                .ToUri();
            using var response = await Server.HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var reader = new StreamReader(responseStream, Encoding.UTF8);
            while (true)
            {
                var readLineTask = reader.ReadLineAsync();
                var delayTask = Task.Delay(35000, cancellationToken);
                await Task.WhenAny(readLineTask, delayTask).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (readLineTask.IsCompleted)
                {
                    var line = readLineTask.Result;
                    if (line == null)
                        return;
                    if (line.Length > 0)
                    {
                        var change = CdbChangeJsonSerializer.Instance.Deserialize(JsonParser.Parse(line));
                        callback(change);
                    }
                }
                else
                    throw new TimeoutException("Listening to CouchDB changes failed with timeout.");
            }
        }

        /// <summary>
        /// Returns document by the specified docid from the specified db. Unless you request a specific revision, the latest revision of the document will always be returned.
        /// </summary>
        /// <param name="docId">Document ID</param>
        /// <param name="attachments">Includes attachments bodies in response. Default is false</param>
        /// <param name="attEncodingInfo">Includes encoding information in attachment stubs if the particular attachment is compressed. Default is false.</param>
        /// <param name="attsSince">Includes attachments only since specified revisions. Doesn’t includes attachments for specified revisions. Optional</param>
        /// <param name="conflicts">Includes information about conflicts in document. Default is false</param>
        /// <param name="deletedConflicts">Includes information about deleted conflicted revisions. Default is false</param>
        /// <param name="latest">Forces retrieving latest “leaf” revision, no matter what rev was requested. Default is false</param>
        /// <param name="localSeq">Includes last update sequence number for the document. Default is false</param>
        /// <param name="meta">Acts same as specifying all conflicts, deleted_conflicts and open_revs query parameters. Default is false</param>
        /// <param name="openRevs">Retrieves documents of specified leaf revisions. Optional</param>
        /// <param name="openRevsAll">Additionally, it accepts value as all to return all leaf revisions. Optional</param>
        /// <param name="rev">Retrieves document of specified revision. Optional</param>
        /// <param name="revs">Includes list of all known document revisions. Default is false</param>
        /// <param name="revsInfo">Includes detailed information for all known document revisions. Default is false</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Document JSON</returns>
        public async Task<ImmutableJson> GetDocumentAsync(string docId, bool attachments = false, bool attEncodingInfo = false, IEnumerable<string>? attsSince = null, bool conflicts = false, bool deletedConflicts = false, bool latest = false, bool localSeq = false, bool meta = false, IEnumerable<string>? openRevs = null, bool openRevsAll = false, string? rev = null, bool revs = false, bool revsInfo = false, CancellationToken cancellationToken = default)
        {
            var url = new CdbQueryBuilder(Name, docId)
                .AppendBoolean("attachments", attachments, false)
                .AppendBoolean("att_encoding_info", attEncodingInfo, false)
                .AppendStringArray("atts_since", attsSince)
                .AppendBoolean("conflicts", conflicts, false)
                .AppendBoolean("deleted_conflicts", deletedConflicts, false)
                .AppendBoolean("latest", latest, false)
                .AppendBoolean("local_seq", localSeq, false)
                .AppendBoolean("meta", meta, false)
                .AppendStringArray("open_revs", openRevs)
                .AppendString("open_revs", openRevsAll ? "all" : null)
                .AppendString("rev", rev)
                .AppendBoolean("revs", revs, false)
                .AppendBoolean("revs_info", revsInfo, false)
                .ToUri();
            using var response = await Server.HttpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.GetJsonResponseAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<T> GetDocumentAsync<T>(IJsonSerializer<T> serializer, string docId, bool attachments = false, bool attEncodingInfo = false, IEnumerable<string>? attsSince = null, bool conflicts = false, bool deletedConflicts = false, bool latest = false, bool localSeq = false, bool meta = false, IEnumerable<string>? openRevs = null, bool openRevsAll = false, string? rev = null, bool revs = false, bool revsInfo = false, CancellationToken cancellationToken = default)
        {
            if (serializer == null)
                throw new ArgumentNullException(nameof(serializer));
            var json = await GetDocumentAsync(docId, attachments, attEncodingInfo, attsSince, conflicts, deletedConflicts, latest, localSeq, meta, openRevs, openRevsAll, rev, revs, revsInfo, cancellationToken).ConfigureAwait(false);
            return serializer.Deserialize(json);
        }

        public async Task<CdbMultipartDocument> GetMultipartDocumentAsync(string docId, bool attachments = false, bool attEncodingInfo = false, IEnumerable<string>? attsSince = null, bool conflicts = false, bool deletedConflicts = false, bool latest = false, bool localSeq = false, bool meta = false, IEnumerable<string>? openRevs = null, bool openRevsAll = false, string? rev = null, bool revs = false, bool revsInfo = false, CancellationToken cancellationToken = default)
        {
            var url = new CdbQueryBuilder(Name, docId)
                .AppendBoolean("attachments", attachments, false)
                .AppendBoolean("att_encoding_info", attEncodingInfo, false)
                .AppendStringArray("atts_since", attsSince)
                .AppendBoolean("conflicts", conflicts, false)
                .AppendBoolean("deleted_conflicts", deletedConflicts, false)
                .AppendBoolean("latest", latest, false)
                .AppendBoolean("local_seq", localSeq, false)
                .AppendBoolean("meta", meta, false)
                .AppendStringArray("open_revs", openRevs)
                .AppendString("open_revs", openRevsAll ? "all" : null)
                .AppendString("rev", rev)
                .AppendBoolean("revs", revs, false)
                .AppendBoolean("revs_info", revsInfo, false)
                .ToUri();
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("multipart/related"));
            using var response = await Server.HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentType.MediaType.StartsWith("multipart", StringComparison.OrdinalIgnoreCase))
            {
                var boundary = response.Content.Headers.ContentType.Parameters.First(nv => nv.Name == "boundary").Value.Trim(' ', '"');
                var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                var reader = new MultipartRelatedReader(boundary, stream);
                await reader.ReadBoundaryAsync(cancellationToken).ConfigureAwait(false);
                await reader.ReadHeadersAsync(cancellationToken).ConfigureAwait(false);
                var jsonText = await reader.ReadTextPartAsync(cancellationToken).ConfigureAwait(false);
                var json = JsonParser.Parse(jsonText);
                var attachmentsList = new List<CdbMultipartDocumentAttachment>();
                while (!reader.Eof)
                {
                    var headers = await reader.ReadHeadersAsync(cancellationToken).ConfigureAwait(false);
                    var contentType = headers["Content-Type"];
                    var contentDisposition = headers["Content-Disposition"];
                    var name = contentDisposition.Substring(contentDisposition.IndexOf("=", StringComparison.Ordinal) + 1).Trim(' ', '"');
                    var size = int.Parse(headers["Content-Length"], CultureInfo.InvariantCulture);
                    var attachmentStream = await reader.ReadBinaryPartAsync(size, cancellationToken).ConfigureAwait(false);
                    attachmentStream.Seek(0, SeekOrigin.Begin);
                    if (headers.TryGetValue("Content-Encoding", out var contentEncoding))
                    {
                        if (contentEncoding == "gzip")
                            attachmentStream = new GZipStream(attachmentStream, CompressionMode.Decompress);
                        else
                            throw new NotSupportedException("Unsupported attachment encoding: " + contentEncoding);
                    }
                    attachmentsList.Add(new CdbMultipartDocumentAttachment(name, contentType, attachmentStream));
                    await reader.ReadBoundaryAsync(cancellationToken).ConfigureAwait(false);
                }
                return new CdbMultipartDocument(json, attachmentsList);
            }
            else
            {
                var json = await response.Content.GetJsonResponseAsync(cancellationToken).ConfigureAwait(false);
                return new CdbMultipartDocument(json, Array.Empty<CdbMultipartDocumentAttachment>());
            }
        }

        public static CdbDocumentInfo GetDocumentInfo(ImmutableJson json)
        {
            return CdbDocumentInfoJsonSerializer.Instance.Deserialize(json);
        }

        public async Task<CdbDocumentOperationResponse> PutDocumentAsync(string docId, ImmutableJson data, CancellationToken cancellationToken = default)
        {
            if (docId == null)
                throw new ArgumentNullException(nameof(docId));
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            using var requestContent = new StringContent(data.ToString(), Encoding.UTF8, "application/json");
            using var response = await Server.HttpClient.PutAsync($"{Name}/{docId}", requestContent, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.GetJsonResponseAsync(CdbDocumentOperationResponseJsonSerializer.Instance, cancellationToken).ConfigureAwait(false);
        }

        public Task<CdbDocumentOperationResponse> PutDocumentAsync<T>(string docId, T data, CancellationToken cancellationToken = default) where T : IJsonSerializable
        {
            return PutDocumentAsync(docId, data.SerializeJson(), cancellationToken);
        }

        public async Task<CdbDocumentOperationResponse> PutMultipartDocumentAsync(string docId, ImmutableJson data, IEnumerable<Stream> attachments, CancellationToken cancellationToken = default)
        {
            if (docId == null)
                throw new ArgumentNullException(nameof(docId));
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (attachments == null)
                throw new ArgumentNullException(nameof(attachments));
            using var requestContent = new MultipartContent("related");
            requestContent.Add(new StringContent(data.ToString(), Encoding.UTF8, "application/json"));
            foreach (var stream in attachments)
            {
                requestContent.Add(new StreamContent(stream));
            }
            using var response = await Server.HttpClient.PutAsync($"{Name}/{docId}", requestContent, cancellationToken).ConfigureAwait(false);
            return await response.Content.GetJsonResponseAsync(CdbDocumentOperationResponseJsonSerializer.Instance, cancellationToken).ConfigureAwait(false);
        }

        public async Task<CdbDocumentOperationResponse> DeleteDocumentAsync(string docId, string rev, CancellationToken cancellationToken = default)
        {
            var url = new CdbQueryBuilder(Name, docId).AppendString("rev", rev).ToUri();
            using var response = await Server.HttpClient.DeleteAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.GetJsonResponseAsync(CdbDocumentOperationResponseJsonSerializer.Instance, cancellationToken).ConfigureAwait(false);
        }

        public async Task<Stream> GetBinaryAttachmentAsync(string docId, string attachmentName, string? rev = null, CancellationToken cancellationToken = default)
        {
            var url = new CdbQueryBuilder(Name, docId, attachmentName)
                .AppendString("rev", rev)
                .ToUri();
            using var response = await Server.HttpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var result = new MemoryStream();
            responseStream.CopyTo(result);
            result.Position = 0;
            return result;
        }
    }
}