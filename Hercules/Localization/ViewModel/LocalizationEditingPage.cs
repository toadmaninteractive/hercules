using Hercules.Documents;
using Hercules.Shell;
using Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Hercules.Localization
{
    public enum ApprovalFilter
    {
        All,
        Approved,
        [Description("Not Approved")]
        NotApproved,
    }

    public enum EmptyFilter
    {
        All,
        Empty,
        [Description("Not Empty")]
        NotEmpty,
    }

    public class LocalizationEntry : NotifyPropertyChanged
    {
        public IDocument Document { get; }
        public JsonPath Path { get; }
        public string PathString { get; }

        private string text;

        public string Text
        {
            get => text;
            set
            {
                SetField(ref text, value);
                if (text != initialText || isApproved != initialApproved)
                    IsModified = true;
            }
        }

        private readonly string initialText;
        private readonly bool initialApproved;

        private bool isApproved;

        public bool IsApproved
        {
            get => isApproved;
            set
            {
                SetField(ref isApproved, value);
                if (text != initialText || isApproved != initialApproved)
                    IsModified = true;
            }
        }

        private bool isModified;

        public bool IsModified
        {
            get => isModified;
            private set => SetField(ref isModified, value);
        }

        public ICommand ApproveCommand { get; }
        public ICommand OpenCommand { get; }

        public LocalizationEntry(IDocument document, JsonPath path, string text, bool isApproved, Action<EditDocumentRequest> editDocument)
        {
            Document = document;
            Path = path;
            this.text = text;
            initialText = text;
            PathString = path.ToString();
            this.isApproved = isApproved;
            initialApproved = isApproved;
            ApproveCommand = Commands.Execute(() => IsApproved = true);
            OpenCommand = Commands.Execute(() => editDocument(new EditDocumentRequest(document, null, Path)));
        }
    }

    public class LocalizationEditingPage : Page
    {
        private readonly Project project;
        private readonly DocumentsModule documentsModule;

        public LocalizationEditingPage(Project project, DocumentsModule documentsModule)
        {
            this.project = project;
            this.documentsModule = documentsModule;
            Title = "Text Editing";
            Refresh();
            SubmitCommand = Commands.Execute(Submit).If(() => !IsSaving);
            RefreshCommand = Commands.Execute(Refresh).If(() => !IsSaving);
            ApproveAllCommand = Commands.Execute(ApproveAll).If(() => !IsSaving);
            SaveCommand = Commands.ExecuteAsync(SaveAsync).If(() => !IsSaving);
        }

        public ObservableCollection<LocalizationEntry> Entries { get; } = new();

        private ApprovalFilter approvalFilter = ApprovalFilter.NotApproved;

        public ApprovalFilter ApprovalFilter
        {
            get => approvalFilter;
            set => SetField(ref approvalFilter, value);
        }

        private EmptyFilter emptyFilter = EmptyFilter.All;

        public EmptyFilter EmptyFilter
        {
            get => emptyFilter;
            set => SetField(ref emptyFilter, value);
        }

        private bool isSaving;
        public bool IsSaving
        {
            get => isSaving;
            private set => SetField(ref isSaving, value);
        }

        public ICommand SubmitCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ApproveAllCommand { get; }

        private void Refresh()
        {
            Entries.Clear();
            var db = project.SchemafulDatabase;
            var schema = db.Schema;
            if (schema == null)
                return;
            var search = new LocalizationSearch(schema, EntryFound);
            foreach (var cat in db.Categories)
                foreach (var doc in cat.Documents)
                {
                    if (doc.Json.TryGetValue("localized", out var localized) && localized.IsBool && localized.AsBool == false)
                        continue;
                    search.AddDocument(doc);
                }
        }

        private void EntryFound(IDocument document, JsonPath path, ImmutableJsonObject data)
        {
            var text = data["text"].AsString;
            var approvedText = data.GetValueSafe("approved_text").AsStringOrNull() ?? "";
            bool isApproved = text == approvedText;

            if (approvalFilter == ApprovalFilter.Approved && !isApproved)
                return;
            if (approvalFilter == ApprovalFilter.NotApproved && isApproved)
                return;

            if (emptyFilter == EmptyFilter.Empty && text != string.Empty)
                return;
            if (emptyFilter == EmptyFilter.NotEmpty && text == string.Empty)
                return;

            var entry = new LocalizationEntry(document, path, text, isApproved, req => documentsModule.EditDocument(req));
            Entries.Add(entry);
        }

        private async Task SaveAsync()
        {
            IsSaving = true;
            try
            {
                List<Task> tasks = new();
                foreach (var pair in Entries.GroupBy(e => e.Document).ToDictionary(k => k.Key, v => v.ToList()))
                {
                    var doc = pair.Key;
                    var json = doc.Json;
                    JsonPath? firstPath = null;
                    foreach (var entry in pair.Value)
                    {
                        if (!entry.IsModified)
                            continue;

                        firstPath ??= entry.Path;
                        var locEntry = json.Fetch(entry.Path);
                        if (locEntry.IsObject)
                        {
                            var mutable = new JsonObject(locEntry.AsObject) { ["text"] = entry.Text };
                            if (entry.IsApproved)
                                mutable["approved_text"] = entry.Text;
                            json = json.Update(entry.Path, mutable.ToImmutable()).AsObject;
                        }
                    }

                    if (firstPath != null)
                    {
                        tasks.Add(SaveDocumentAsync(doc, json));
                    }
                }

                await Task.WhenAll(tasks);
            }
            finally
            {
                IsSaving = false;
            }
            CommandManager.InvalidateRequerySuggested();
        }

        private async Task SaveDocumentAsync(IDocument document, ImmutableJsonObject json)
        {
            var metadata = new MetadataDraft(project.Connection.Username);
            var draft = new DocumentDraft(json, document.Attachments);
            await project.Database.SaveDocumentAsync(document, draft, metadata);
            Entries.RemoveAll(e => e.Document == document);
        }

        private void Submit()
        {
            var requests = new List<EditDocumentRequest>();
            foreach (var group in Entries.GroupBy(e => e.Document))
            {
                var doc = group.Key;
                var json = doc.Json;
                JsonPath? firstPath = null;
                foreach (var entry in group)
                {
                    if (!entry.IsModified)
                        continue;

                    firstPath ??= entry.Path;
                    var locEntry = json.Fetch(entry.Path);
                    if (locEntry.IsObject)
                    {
                        var mutable = new JsonObject(locEntry.AsObject) { ["text"] = entry.Text };
                        if (entry.IsApproved)
                            mutable["approved_text"] = entry.Text;
                        json = json.Update(entry.Path, mutable.ToImmutable()).AsObject;
                    }
                }

                if (firstPath != null)
                {
                    requests.Add(new EditDocumentRequest(doc, json, firstPath));
                }
            }
            documentsModule.EditDocuments(requests);
        }

        private void ApproveAll()
        {
            foreach (var entry in Entries)
                entry.IsApproved = true;
        }
    }
}
