using System;

namespace Hercules.Documents
{
    public abstract class Attachment
    {
        public string DocumentId { get; }
        public string Name { get; }
        public long Length { get; }
        public string ContentType { get; }
        public IFile File { get; }

        protected Attachment(string documentId, string name, long length, string contentType, IFile file)
        {
            DocumentId = documentId;
            Name = name;
            Length = length;
            ContentType = contentType;
            File = file;
        }
    }

    public class AttachmentRevision : Attachment
    {
        public int RevPos { get; }
        public string Digest { get; }

        public AttachmentRevision(string documentId, string name, long length, int revPos, string contentType, string digest, AttachmentFile file)
            : base(documentId, name, length, contentType, file)
        {
            this.RevPos = revPos;
            this.Digest = digest;
        }
    }

    public class AttachmentDraft : Attachment
    {
        public AttachmentDraft(string documentId, string name, long length, string contentType, IFile file)
            : base(documentId, name, length, contentType, file)
        {
        }

        public AttachmentDraft(string documentId, string name, TempFile file)
            : this(documentId, name, file.Length, MimeMapping.MimeUtility.GetMimeMapping(file.FileName), file)
        {
        }

        public static AttachmentDraft CopyFrom(Attachment source, TempStorage tempStorage)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(tempStorage);
            if (!source.File.IsLoaded)
                throw new InvalidOperationException("Source file is not loaded");

            var tempFile = tempStorage.CreateFile(source.Name, source.File.FileName!);
            return new AttachmentDraft(source.DocumentId, source.Name, source.Length, source.ContentType, tempFile);
        }
    }
}
