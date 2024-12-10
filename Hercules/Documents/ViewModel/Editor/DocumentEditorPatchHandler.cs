using Hercules.Forms.Elements;
using Hercules.Forms.Schema;
using Hercules.Shell;
using Hercules.Shortcuts;
using Json;
using System;
using System.Linq;
using System.Windows.Input;

namespace Hercules.Documents.Editor
{
    public sealed class DocumentEditorPatchHandler : NotifyPropertyChanged, IFormService, IOnApplySchema, IDisposable
    {
        private readonly ObservableValue<bool> isModified;
        private readonly IDocument document;
        private readonly DocumentFormTab formTab;
        private readonly NotificationService notifications;
        private SchemafulDatabase schemafulDatabase;
        private Category category;

        public IReadOnlyObservableValue<bool> IsModified => isModified;

        public ICommand UnlinkCommand { get; }
        public ICommand LinkCommand { get; }

        public ImmutableJsonObject? OriginalBaseJson { get; private set; }
        public ImmutableJsonObject? BaseJson { get; private set; }
        public IDocument? BaseDocument { get; private set; }
        public IShortcut? BaseDocumentShortcut => BaseDocument?.Shortcut;

        public bool IsPatch => BaseJson != null;

        public DocumentEditorPatchHandler(IDocument document, DocumentFormTab formTab, NotificationService notifications, SchemafulDatabase schemafulDatabase, Category category)
        {
            this.document = document;
            this.formTab = formTab;
            this.notifications = notifications;
            this.schemafulDatabase = schemafulDatabase;
            this.category = category;
            isModified = new ObservableValue<bool>(false);
            OriginalBaseJson = CouchUtils.GetBase(document.Json);

            UnlinkCommand = Commands.Execute(Unlink).If(() => IsPatch);
            LinkCommand = Commands.Execute(ShowLinkNotification).If(() => !IsPatch && this.category.IsSchemaful);

            UpdateBaseJson(OriginalBaseJson);
        }

        private void ShowLinkNotification()
        {
            notifications.RemoveAll<LinkToDocumentNotification>();
            var docs = category.Documents.Where(doc => doc.IsExisting && doc.DocumentId != document.DocumentId).OrderBy(doc => doc.DocumentId);
            notifications.Show(new LinkToDocumentNotification(docs.ToList(), Link, this));
        }

        private void Unlink()
        {
            BaseDocument!.OnChanged -= BaseDocument_OnChanged;
            BaseDocument = null;
            BaseJson = null;
            RaisePropertyChanged(nameof(IsPatch));
            RaisePropertyChanged(nameof(BaseDocument));
            RaisePropertyChanged(nameof(BaseDocumentShortcut));
            isModified.Value = OriginalBaseJson != null;
            formTab.Form.Root.Visit(element => element.IsInherited = false, VisitOptions.None);
            formTab.Form.RemoveService<DocumentEditorPatchHandler>();
        }

        private void Link(IDocument newBase)
        {
            if (!newBase.IsExisting)
                return;

            if (IsPatch)
            {
                Unlink();
            }

            BaseDocument = newBase;
            BaseDocument.OnChanged += BaseDocument_OnChanged;
            BaseJson = newBase.Json.WithoutKeys(CouchUtils.BaseJsonExcludedKeys);
            isModified.Value = OriginalBaseJson == null || !ImmutableJson.Equals(OriginalBaseJson, BaseJson);
            formTab.Form.AddService(this);
            formTab.Form.Override(BaseJson!);
            RaisePropertyChanged(nameof(IsPatch));
            RaisePropertyChanged(nameof(BaseDocument));
            RaisePropertyChanged(nameof(BaseDocumentShortcut));
        }

        public void ProcessTransaction(ITransaction transaction, TransactionStage stage)
        {
            if (stage == TransactionStage.Cleanup)
            {
                formTab.Form.Override(BaseJson!);
            }
        }

        private void BaseDocument_OnChanged(DocumentChange change)
        {
            switch (change)
            {
                case DocumentChange.RemoteDeleted:
                case DocumentChange.UserDeleted:
                case DocumentChange.RemoteUpdated:
                    CheckIfBaseDocumentChanged();
                    break;
            }
        }

        private void CheckIfBaseDocumentChanged()
        {
            if (!BaseDocument!.IsExisting)
            {
                notifications.RemoveBySource(this);
                notifications.AddMessage($"Unlinked from base document {BaseDocument!.DocumentId}: base document does not exist.", MessageNotificationType.Warning, this);
                Unlink();
                return;
            }

            schemafulDatabase.GetDocumentSchema(BaseDocument, BaseJson!, out var baseCategory, out var baseSchema);
            if (baseCategory != category)
            {
                notifications.RemoveBySource(this);
                notifications.AddMessage($"Unlinked from base document {BaseDocument!.DocumentId}: base document category has changed.", MessageNotificationType.Warning, this);
                Unlink();
                return;
            }

            if (!BaseDocument.Json.WithoutKeys(CouchUtils.BaseJsonExcludedKeys).Equals(BaseJson))
            {
                notifications.RemoveBySource(this);
                var message = $"New version of base document {BaseDocument.DocumentId} is available.";
                notifications.Show(new RebaseDocumentNotification(message, Rebase, this));
            }
            else
            {
                notifications.RemoveAll<RebaseDocumentNotification>();
            }
        }

        private void Rebase()
        {
            if (BaseDocument != null)
            {
                var schemaType = schemafulDatabase.Schema!.DocumentRoot(category.Name);
                var oldBase = schemaType.ConstrainJson(BaseJson).AsObject;
                var newBase = schemaType.ConstrainJson(BaseDocument.Json).AsObject;
                BaseJson = BaseDocument.Json.WithoutKeys(CouchUtils.BaseJsonExcludedKeys);
                isModified.Value = true;
                if (!oldBase.Equals(newBase))
                {
                    var oldJson = formTab.DraftJson; // constrain is not needed?
                    var newJson = schemaType.ConstrainedRebase(oldJson, oldBase, newBase);
                    var result = new JsonObject(newJson.AsObject) { [schemafulDatabase.Schema.Variant.Tag] = category.Name };
                    formTab.UpdateJson(result);
                }
            }
        }

        public void Saved()
        {
            OriginalBaseJson = BaseJson;
            isModified.Value = false;
        }

        public void RevertToBase(Element element)
        {
            if (BaseJson != null)
            {
                BaseJson.TryFetch(element.ValuePath, out var baseJsonValue);
                formTab.RunAndLogWarnings(transaction => element.SetJson(baseJsonValue, transaction));
            }
        }

        public void Dispose()
        {
            isModified.Dispose();
            if (BaseDocument != null)
                BaseDocument.OnChanged -= BaseDocument_OnChanged;
        }

        public void OnApplySchema(SchemafulDatabase schemafulDatabase, Category category, SchemaRecord schemaRecord)
        {
            this.schemafulDatabase = schemafulDatabase;
            this.category = category;
            if (BaseDocument != null)
            {
                schemafulDatabase.GetDocumentSchema(BaseDocument, BaseJson!, out var baseCategory, out var baseSchema);
                if (baseCategory != category)
                {
                    notifications.RemoveBySource(this);
                    notifications.AddMessage($"Unlinked from base document {BaseDocument!.DocumentId}: category changed.", MessageNotificationType.Warning, this);
                    Unlink();
                }
                else
                {
                    formTab.Form.Override(BaseJson!);
                }
            }
        }

        public void UpdateRemote(ImmutableJsonObject json, bool takeRemote)
        {
            OriginalBaseJson = CouchUtils.GetBase(json);
            if (takeRemote)
                UpdateBaseJson(OriginalBaseJson);
            else
                isModified.Value = !ImmutableJson.Equals(OriginalBaseJson, BaseJson);
        }

        private void UpdateBaseJson(ImmutableJsonObject? json)
        {
            var patchInstalled = IsPatch;
            if (BaseDocument != null)
                BaseDocument.OnChanged -= BaseDocument_OnChanged;
            BaseDocument = null;
            BaseJson = null;
            if (json != null)
            {
                if (json.TryGetValue("_id", out var baseDocId) && baseDocId.IsString)
                {
                    if (schemafulDatabase.AllDocuments.TryGetValue(baseDocId.AsString, out var baseDocument))
                    {
                        BaseDocument = baseDocument;
                        BaseJson = json;
                        BaseDocument.OnChanged += BaseDocument_OnChanged;
                    }
                    else
                    {
                        notifications.AddMessage($"Unlinked from base document {baseDocId.AsString}: base document does not exist.", MessageNotificationType.Warning, this);
                    }
                }
                else
                {
                    notifications.AddMessage("Unlinked from base document: malformed base document content.", MessageNotificationType.Error, this);
                }
            }
            if (patchInstalled && !IsPatch)
            {
                formTab.Form.Root.Visit(element => element.IsInherited = false, VisitOptions.None);
                formTab.Form.RemoveService<DocumentEditorPatchHandler>();
            }
            if (!patchInstalled && IsPatch)
            {
                formTab.Form.AddService(this);
                formTab.Form.Override(BaseJson!);
            }
            isModified.Value = !ImmutableJson.Equals(OriginalBaseJson, BaseJson);
            RaisePropertyChanged(nameof(IsPatch));
            RaisePropertyChanged(nameof(BaseDocument));
            RaisePropertyChanged(nameof(BaseDocumentShortcut));
            if (IsPatch)
            {
                CheckIfBaseDocumentChanged();
            }
        }
    }
}
