using Hercules.Forms.Elements;

namespace Hercules.Dialogs
{
    public class VirtualReplicaViewModel : BaseReplicaViewModel
    {
        public string ReplicaId { get; set; }
        public ReplicaViewModel RelativeReplica { get; set; }

        public VirtualReplicaViewModel(string replicaId, ReplicaListItem element)
        {
            ReplicaId = replicaId;
            Element = element;
        }

        public override void SetSelectedState(bool value)
        {
            base.SetSelectedState(value);
            RelativeReplica.IsBlurAnchor = value;
        }
    }
}