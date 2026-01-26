using Hercules.Documents.Editor;
using Hercules.Forms.Elements;
using Hercules.Shell;
using Json;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace Hercules.Dialogs
{
    /// <summary>
    /// Dialogs ViewModel
    /// </summary>
    public class DialogTab : PageTab
    {
        private bool isBlur;

        public bool IsBlur
        {
            get => isBlur;
            set => SetField(ref isBlur, value);
        }

        private bool isAnchorSelectState;

        public bool IsAnchorSelectState
        {
            get => isAnchorSelectState;
            set => SetField(ref isAnchorSelectState, value);
        }

        public ObservableCollection<ReplicaViewModel> DialogRoot { get; } = new();

        public DocumentEditorPage Editor { get; }
        public ICommand AddElement { get; }
        public ICommand AddAnchor { get; }
        public ICommand RemoveElement { get; }
        public ICommand UpElement { get; }
        public ICommand DownElement { get; }

        private readonly IDisposable elementListSubscription;
        private readonly DocumentForm form;
        private readonly RefElement startElement;
        private readonly PropertyEditorTool propertyEditorTool;
        private ReplicaViewModel anchor;
        private bool LockUpdateDialogTree;
        private readonly DialogTree dialogTree = new DialogTree();

        public DialogTab(DocumentEditorPage editor, PropertyEditorTool propertyEditorTool)
        {
            this.Title = "Dialog";
            this.Editor = editor;
            this.propertyEditorTool = propertyEditorTool;

            #region Command

            AddElement = Commands.Execute<ReplicaViewModel>(DoAddElement);
            AddAnchor = Commands.Execute<ReplicaViewModel>(DoAddAnchor);
            RemoveElement = Commands.Execute<BaseReplicaViewModel>(DoRemoveElement);
            UpElement = Commands
                .Execute<BaseReplicaViewModel>(replica => DoUpDownElement(replica))
                .If(replica => replica.Parent != null && replica.Parent.Answers.IndexOf(replica) > 0);
            DownElement = Commands
                .Execute<BaseReplicaViewModel>(replica => DoUpDownElement(replica, false))
                .If(replica => replica.Parent != null && replica.Parent.Answers.IndexOf(replica) < replica.Parent.Answers.Count - 1);

            #endregion Command

            form = editor.FormTab.Form;
            startElement = (RefElement)form.Root.GetByPath(new JsonPath("start"));
            startElement.PropertyChanged +=
                (sender, e) => { if (e.PropertyName == "Value") RefreshDialogTree(); };

            var replicasList = form.Root.GetByPath(new JsonPath("replicas")) as ListElement;
            var elements = replicasList.Children.Cast<ReplicaListItem>();
            dialogTree.MakeTree(elements, startElement.Value, DialogRoot);

            foreach (ReplicaListItem element in elements)
                element.PropertyChanged += ElementChanged;

            elementListSubscription?.Dispose();

            elementListSubscription = replicasList.Children.OnCollectionChanged().Subscribe(ElementListChanged);
        }

        private void ElementListChanged(NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (ReplicaListItem element in e.NewItems!.Cast<ReplicaListItem>())
                    {
                        if (string.IsNullOrEmpty(element.RefValue.Value))
                            element.RefValue.Value = Guid.NewGuid().ToString();

                        element.PropertyChanged += ElementChanged;
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (ReplicaListItem element in e.OldItems!.Cast<ReplicaListItem>())
                    {
                        element.PropertyChanged -= ElementChanged;
                        if (!LockUpdateDialogTree)
                        {
                            var replica = dialogTree.GetReplicaById(element.RefValue.Value);
                            DoRemoveElement(replica);
                        }
                    }
                    break;
            }
        }

        private void ElementChanged(object? sender, PropertyChangedEventArgs e)
        {
            var element = (ReplicaListItem)sender;
            if (e.PropertyName == nameof(ReplicaListItem.AnswerIds))
                RefreshDialogTree();
        }

        public void ApplySchema()
        {
            throw new NotImplementedException();
        }

        public void AddVirtualRepliva(ReplicaViewModel replica)
        {
            anchor.IsContentEnabled = true;
            IsAnchorSelectState = false;

            var jsonAnswer = new JsonObject { ["ref"] = replica.Element.RefValue.Value, ["is_virtual"] = true };

            form.Run(transaction => anchor.Element.AnswerList.PasteElement(jsonAnswer, transaction));
            anchor = null;
        }

        private void DoAddAnchor(ReplicaViewModel parametr)
        {
            if (anchor != null)
                anchor.IsContentEnabled = true;

            anchor = parametr;
            anchor.IsContentEnabled = false;
            IsAnchorSelectState = true;
        }

        private void DoAddElement(ReplicaViewModel parametr)
        {
            string newGuid = Guid.NewGuid().ToString();

            var json = new JsonObject { ["ref"] = newGuid, ["text"] = new JsonObject(), ["answers"] = new JsonObject() };

            var jsonAnswer = new JsonObject { ["ref"] = newGuid, ["is_virtual"] = false };

            form.Run(transaction =>
            {
                transaction.RequestFullInvalidation();
                var item = parametr.Element.List.PasteElement(json, transaction);
                dialogTree.MakeReplicaItem(parametr, (ReplicaListItem)item);
                parametr.Element.AnswerList.PasteElement(jsonAnswer, transaction);
            });

            RefreshDialogTree();
            var newReplica = dialogTree.GetReplicaById(newGuid);
            newReplica.IsEditMode = true;
        }

        private void DoRemoveElement(BaseReplicaViewModel replica)
        {
            LockUpdateDialogTree = true;
            form.Run(transaction =>
            {
                if (replica.Parent != null) // it is not Root element
                    replica.Parent.Element.AnswerList.Remove(replica.AnswerElement, transaction);
                else
                    startElement.Value = string.Empty;

                if (!replica.IsVirtual)
                {
                    foreach (var virtualReplic in dialogTree.FindVirtualReplicaForReplica(replica as ReplicaViewModel))
                        virtualReplic.Parent.Element.AnswerList.Remove(virtualReplic.AnswerElement, transaction);

                    replica.Element.List.Remove(replica.Element, transaction);
                }
            });
            LockUpdateDialogTree = false;
            RefreshDialogTree();
        }

        private void DoUpDownElement(BaseReplicaViewModel replica, bool moveUp = true)
        {
            if (moveUp) replica.Parent.Answers.MoveUp(replica);
            else replica.Parent.Answers.MoveDown(replica);

            var newAnswers = new JsonArray();

            foreach (BaseReplicaViewModel answer in replica.Parent.Answers)
            {
                var jsonAnswer = new JsonObject { ["ref"] = answer.Element.RefValue.Value, ["is_virtual"] = answer is VirtualReplicaViewModel };
                newAnswers.Add(jsonAnswer);
            }

            LockUpdateDialogTree = true;
            form.Run(transaction => replica.Parent.Element.AnswerList.SetJson(newAnswers, transaction));
            LockUpdateDialogTree = false;
            RefreshDialogTree();
        }

        public override void OnActivate()
        {
            propertyEditorTool.Show();
        }

        private void RefreshDialogTree()
        {
            if (LockUpdateDialogTree)
                return;

            var replicasList = form.Root.GetByPath(new JsonPath("replicas")) as ListElement;
            if (replicasList == null)
                return;
            var elements = replicasList.Children.Cast<ReplicaListItem>();
            dialogTree.MakeTree(elements, startElement.Value, DialogRoot);
        }
    }
}
