using Hercules.Forms.Elements;
using Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Hercules.Dialogs
{
    public class DialogTree
    {
        private readonly List<ReplicaViewModel> actualReplicaList = new();
        private readonly List<VirtualReplicaViewModel> actualVirtualReplicaList = new();
        private List<string> usingElementIds = new();
        private IEnumerable<ReplicaListItem> source;

        public void MakeTree(IEnumerable<ReplicaListItem> replicas, string startRef, ObservableCollection<ReplicaViewModel> dialogRoot)
        {
            dialogRoot.Clear();
            if (string.IsNullOrWhiteSpace(startRef))
                return;

            actualReplicaList.Clear();
            actualVirtualReplicaList.Clear();
            usingElementIds = new List<string>();
            source = replicas;

            var startElement = source.Single(x => x.RefValue.Value == startRef);
            var root = MakeReplicaItem(null, startElement);

            foreach (var virtualReplica in actualVirtualReplicaList)
                virtualReplica.RelativeReplica = actualReplicaList.Single(x => x.Element.RefValue.Value == virtualReplica.ReplicaId);

            dialogRoot.Add(root);
        }

        public ReplicaViewModel MakeReplicaItem(ReplicaViewModel? parent, ReplicaListItem element)
        {
            usingElementIds.Add(element.RefValue.Value);
            var answers = new ObservableCollection<BaseReplicaViewModel>();
            ReplicaViewModel viewModel = new ReplicaViewModel(element);

            if (element.IsValid)
            {
                foreach (var answerElement in element.AnswersElements)
                {
                    var id = ((RefElement)answerElement.GetByPath(new JsonPath("ref"))).Value!;
                    var isVirtual = ((BoolElement)answerElement.GetByPath(new JsonPath("is_virtual"))).Value!;

                    if (isVirtual || usingElementIds.Contains(id))
                    {
                        var answerVirtualReplica = new VirtualReplicaViewModel(id, source.Single(x => x.RefValue.Value == id))
                        {
                            Parent = viewModel,
                            AnswerElement = answerElement
                        };
                        answers.Add(answerVirtualReplica);
                        actualVirtualReplicaList.Add(answerVirtualReplica);
                    }
                    else
                    {
                        var answerReplica = MakeReplicaItem(viewModel, source.Single(x => x.RefValue.Value == id));
                        answerReplica.AnswerElement = answerElement;
                        answers.Add(answerReplica);
                    }
                }
            }

            viewModel.Parent = parent;
            viewModel.Answers.Clear();
            viewModel.Answers.AddRange(answers);

            actualReplicaList.Add(viewModel);
            return viewModel;
        }

        public ReplicaViewModel GetReplicaById(string replicaRef)
        {
            return actualReplicaList.Single(x => x.Element.RefValue.Value == replicaRef);
        }

        public List<VirtualReplicaViewModel> FindVirtualReplicaForReplica(ReplicaViewModel replica)
        {
            return actualVirtualReplicaList.Where(x =>
                x.RelativeReplica.CheckHierarchicalParent(replica) ||
                x.RelativeReplica == replica).ToList();
        }
    }
}
