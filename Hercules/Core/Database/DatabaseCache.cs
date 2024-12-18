using Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace Hercules.DB
{
    public class DatabaseCache : IDatabaseCache
    {
        string FilenameLastSeq => GetPath("last_seq");

        const string FilenameRevision = "rev";

        public string RootPath { get; }
        public string DesignPath { get; }

        string GetPath(params string[] paths)
        {
            return Path.Combine(RootPath, Path.Combine(paths));
        }

        public DatabaseCache(string path)
        {
            ArgumentNullException.ThrowIfNull(path);

            RootPath = path;
            DesignPath = GetPath("_design");
            EnsureDirectory(RootPath);
            EnsureDirectory(DesignPath);
        }

        public string ReadLastSequence()
        {
            string lastSeq = "0";

            if (File.Exists(FilenameLastSeq))
            {
                return File.ReadAllText(FilenameLastSeq).Trim();
            }

            return lastSeq;
        }

        public void WriteLastSequence(string seq)
        {
            File.WriteAllText(FilenameLastSeq, seq);
        }

        void EnsureDirectory(params string[] dirs)
        {
            var path = GetPath(dirs);
            Directory.CreateDirectory(path);
        }

        public IEnumerable<CacheEntry> LoadCache(CancellationToken cancellationToken)
        {
            var dirInfo = new DirectoryInfo(RootPath);
            var ids = dirInfo.GetDirectories().Select(i => i.Name).Where(name => !name.StartsWith("_", StringComparison.Ordinal));
            var designDirInfo = new DirectoryInfo(DesignPath);
            var designIds = designDirInfo.GetDirectories().Select(i => "_design/" + i.Name);
            return ids.Concat(designIds).AsParallel().WithCancellation(cancellationToken).Select(ReadDocument);
        }

        CacheEntry ReadDocument(string docId)
        {
            var revFile = GetPath(docId, FilenameRevision);
            var revision = File.ReadAllText(revFile);
            var path = GetPath(docId, revision);
            try
            {
                var json = FileUtils.LoadJsonFromFile(path).AsObject;
                return new CacheEntry(docId, revision, json);
            }
            catch (JsonException exception)
            {
                throw new CorruptedCacheEntryException(path, exception);
            }
        }

        public void WriteRevision(string docId, string revision)
        {
            ArgumentNullException.ThrowIfNull(docId);
            ArgumentNullException.ThrowIfNull(revision);

            EnsureDirectory(docId);
            File.WriteAllText(GetPath(docId, FilenameRevision), revision);
        }

        public string? ReadRevision(string docId)
        {
            ArgumentNullException.ThrowIfNull(docId);

            var path = GetPath(docId, FilenameRevision);
            if (File.Exists(path))
                return File.ReadAllText(path);
            else
                return null;
        }

        public void WriteDocument(ImmutableJsonObject data)
        {
            ArgumentNullException.ThrowIfNull(data);

            var id = CouchUtils.GetId(data);
            var revision = CouchUtils.GetRevision(data);
            EnsureDirectory(id);
            File.WriteAllText(GetPath(id, revision), data.ToString());
        }

        public ImmutableJsonObject? TryReadDocument(string docId, string revision)
        {
            ArgumentNullException.ThrowIfNull(docId);
            ArgumentNullException.ThrowIfNull(revision);

            var path = GetPath(docId, revision);
            if (File.Exists(path))
                return FileUtils.LoadJsonFromFile(path).AsObject;
            else
                return null;
        }

        public string SaveAttachment(string docId, string attachmentName, int revpos, Stream stream)
        {
            var fileName = AttachmentFileName(docId, attachmentName, revpos);
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            using var fileStream = File.Create(fileName);
            stream.CopyTo(fileStream);
            return fileName;
        }

        string AttachmentFileName(string docId, string attachmentName, int revpos)
        {
            return GetPath(docId, "attachments", revpos.ToString(CultureInfo.InvariantCulture), attachmentName);
        }

        public void DeleteDocument(string docId)
        {
            ArgumentNullException.ThrowIfNull(docId);

            Directory.Delete(GetPath(docId), true);
        }

        public void Reset()
        {
            Directory.Delete(RootPath, true);
            EnsureDirectory(RootPath);
        }

        public bool TryGetAttachmentFile(string docId, string attachmentName, int revpos, out string fileName)
        {
            fileName = AttachmentFileName(docId, attachmentName, revpos);
            return File.Exists(fileName);
        }
    }
}
