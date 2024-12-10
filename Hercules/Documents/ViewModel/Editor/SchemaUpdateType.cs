using System.ComponentModel;

namespace Hercules.Documents.Editor
{
    public enum SchemaUpdateType
    {
        [Description("Update schema instantly without asking user confirmation")]
        Silent,

        [Description("Show confirmation dialog")]
        Confirmation,

        [Description("Show notification in notification menu")]
        Notification,
    }
}
