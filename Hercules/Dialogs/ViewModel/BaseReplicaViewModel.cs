using Hercules.Forms.Elements;

namespace Hercules.Dialogs
{
    public abstract class BaseReplicaViewModel : NotifyPropertyChanged
    {
        public ReplicaViewModel? Parent { get; set; }
        public ReplicaListItem Element { get; set; }
        public ListItem AnswerElement { get; set; }
        public bool IsVirtual => this is VirtualReplicaViewModel;

        private bool isHierarchicalSelected;

        public bool IsHierarchicalSelected
        {
            get => isHierarchicalSelected;
            set => SetField(ref isHierarchicalSelected, value);
        }

        public virtual void SetSelectedState(bool value)
        {
            IsHierarchicalSelected = value;
            Parent?.SetSelectedState(value);
        }

        public bool CheckHierarchicalParent(ReplicaViewModel expectedParent)
        {
            if (this.Parent == null)
                return false;

            if (this.Parent == expectedParent)
                return true;

            return this.Parent.CheckHierarchicalParent(expectedParent);
        }
    }
}
