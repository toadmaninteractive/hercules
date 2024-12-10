using Hercules.Forms.Elements;
using System.Collections.ObjectModel;

namespace Hercules.Dialogs
{
    public class ReplicaViewModel : BaseReplicaViewModel
    {
        public ObservableCollection<BaseReplicaViewModel> Answers { get; } = new();

        private bool isEditMode;

        public bool IsEditMode
        {
            get => isEditMode;
            set => SetField(ref isEditMode, value);
        }

        private bool isBlurAnchor;

        public bool IsBlurAnchor
        {
            get => isBlurAnchor;
            set => SetField(ref isBlurAnchor, value);
        }

        private bool isContentEnabled = true;

        public bool IsContentEnabled
        {
            get => isContentEnabled;
            set => SetField(ref isContentEnabled, value);
        }

        public ReplicaViewModel(ReplicaListItem element)
        {
            Element = element;
        }

        public override void SetSelectedState(bool value)
        {
            base.SetSelectedState(value);

            if (!value)
                IsEditMode = false;
        }
    }
}