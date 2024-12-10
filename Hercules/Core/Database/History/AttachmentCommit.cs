using Hercules.Documents;

namespace Hercules.DB
{
    public enum AttachmentCommitType
    {
        None,
        Added,
        Updated,
        Removed,
    }

    public sealed class AttachmentCommit
    {
        public AttachmentCommitType ChangeType { get; }
        public AttachmentRevision Attachment { get; }

        public bool IsNew => ChangeType == AttachmentCommitType.Added;
        public bool IsDeleted => ChangeType == AttachmentCommitType.Removed;
        public bool IsUpdated => ChangeType == AttachmentCommitType.Updated;

        public AttachmentCommit(AttachmentCommitType changeType, AttachmentRevision attachment)
        {
            ChangeType = changeType;
            Attachment = attachment;
        }

        public static AttachmentCommit CreateDiff(AttachmentRevision? a1, AttachmentRevision? a2)
        {
            return (a1, a2) switch
            {
                (null, _) => new AttachmentCommit(AttachmentCommitType.Added, a2),
                (_, null) => new AttachmentCommit(AttachmentCommitType.Removed, a1),
                _ when a1 == a2 => new AttachmentCommit(AttachmentCommitType.None, a1),
                _ => new AttachmentCommit(AttachmentCommitType.Updated, a2)
            };
        }
    }
}
