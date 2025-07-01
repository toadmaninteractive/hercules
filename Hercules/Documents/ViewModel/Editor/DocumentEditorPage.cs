using Hercules.Forms.Elements;
using Hercules.Forms.Schema;
using Hercules.Shell;
using Json;
using JsonDiff;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Hercules.Documents.Editor
{
    public interface IOnApplySchema
    {
        void OnApplySchema(SchemafulDatabase schemafulDatabase, Category category, SchemaRecord schema);
    }

    public enum DestinationTab
    {
        Default,
        Form,
        Source,
    }

    public class DocumentEditorPage : TabbedPage, IDocumentEditor
    {
        private readonly DocumentEditorContext context;
        private readonly IDocumentProxy proxy;
        public IDocument Document { get; }
        public DocumentEditorPatchHandler PatchHandler { get; }
        public DocumentFormTab FormTab { get; }
        public JsonSourceTab SourceTab { get; }
        public DocumentAttachments Attachments { get; }
        public ICommand ConvertCategoryCommand { get; }
        public ICommand RunScriptCommand { get; }
        public SchemafulDatabase SchemafulDatabase { get; private set; }
        public ImmutableJsonObject OriginalData { get; private set; }
        public Category Category { get; private set; }
        public SchemaRecord Schema { get; private set; }
        public bool Enabled
        {
            get => enabled;
            private set => SetField(ref enabled, value);
        }

        public bool IsScript => Category.Type == CategoryType.Script;

        private readonly IDisposable databaseSubscription;
        private readonly IDisposable dirtySubscription;
        private bool enabled = true;
        private readonly ObservableValue<bool> originalIsDirty = new(false);
        private readonly IReadOnlyObservableValue<bool> isModified;

        public DocumentEditorPage(DocumentEditorContext context, IDocument document)
        {
            Document = document;
            this.context = context;
            this.proxy = context.GetDocumentProxy(document, this);
            Title = Document.DocumentId;
            SchemafulDatabase = context.ObservableSchemafulDatabase.Value;

            OriginalData = SanitizeData(document.Json);
            SchemafulDatabase.GetDocumentSchema(document, document.Json, out var category, out var schemaRecord);
            Category = category;
            Schema = schemaRecord;
            Attachments = new DocumentAttachments(document.DocumentId, this.context.TempStorage);
            Attachments.Setup(document.Attachments);

            originalIsDirty.Value = !document.IsExisting;

            var elementFactory = context.GetElementFactory(new ElementFactoryContext(this));
            Tabs.Add(FormTab = new DocumentFormTab(this, context.FormSettings, elementFactory, OriginalData, OriginalData, Schema));
            Tabs.Add(SourceTab = new JsonSourceTab(this));

            PatchHandler = new DocumentEditorPatchHandler(document, FormTab, Notifications, SchemafulDatabase, Category);

            isModified = ObservableValues.Any(originalIsDirty, FormTab.Form.IsModified, Attachments.IsModified, PatchHandler.IsModified);
            dirtySubscription = isModified.Subscribe(ModifiedChanged);
            IsDirty = isModified.Value;
            Status = Document.IsExisting ? "" : "New";

            databaseSubscription = this.context.ObservableSchemafulDatabase.Subscribe(this.SchemaChanged);

            RoutedCommandBindings.Add(DocumentsModule.SaveDocument, Commands.ExecuteAsync(ValidateAndSave).If(() => IsDirty && Enabled));

            ConvertCategoryCommand = Commands.Execute(ConvertCategory).If(() => SchemafulDatabase.Schema != null);
            RunScriptCommand = Commands.Execute(RunScript).If(() => Category.Type == CategoryType.Script);

            GoTo(DestinationTab.Form);
            Services.Add(PatchHandler);
        }

        public override object? GetCommandParameter(Type type)
        {
            return type == typeof(IDocument) ? Document : base.GetCommandParameter(type);
        }

        public void GoTo(DestinationTab destinatonTab, JsonPath? path = null)
        {
            switch (destinatonTab)
            {
                case DestinationTab.Form:
                    ActiveTab = FormTab;
                    if (path != null)
                        FormTab.GoToPath(path);
                    break;

                case DestinationTab.Source:
                    ActiveTab = SourceTab;
                    if (path != null)
                        SourceTab.GoToPath(path);
                    break;

                default:
                    ActiveTab = FormTab;
                    if (path != null)
                        FormTab.GoToPath(path);
                    break;
            }
        }

        public void SetupDraft(DocumentDraft draft)
        {
            Attachments.Setup(draft.Attachments);
            UpdateData(draft.Json);
        }

        public void UpdateData(ImmutableJsonObject data)
        {
            SchemafulDatabase.GetDocumentSchema(Document, data, out var newCategory, out var newSchema);
            if (newCategory != Category || Schema != newSchema)
            {
                originalIsDirty.Value = true;
                Setup(data, newCategory, newSchema);
            }
            else
                FormTab.UpdateJson(SanitizeData(data));
        }

        public void ApplyPatch(JsonPatch patch)
        {
            var json = FormTab.DraftJson;
            json = patch.Apply(json).AsObject;
            UpdateData(json);
        }

        public DocumentDraft GetDocumentDraft()
        {
            var formDraft = FormTab.DraftJson;
            if (PatchHandler.BaseJson != null)
            {
                formDraft = new JsonObject(formDraft) { [CouchUtils.HerculesBase] = PatchHandler.BaseJson };
            }
            return new DocumentDraft(formDraft, Attachments.GetDraft());
        }

        public async Task Save()
        {
            var draft = GetDocumentDraft();
            Operation = "Saving";
            Enabled = false;

            try
            {
                var newRevision = await proxy.SaveDocumentAsync(draft).ConfigureAwait(true);
                Status = "Saved";
                Operation = string.Empty;
                OriginalData = SanitizeData(newRevision.Json);
                originalIsDirty.Value = false;
                PatchHandler.Saved();
                Attachments.Setup(newRevision.Attachments);
                FormTab.Form.SetOriginalJson(OriginalData);
            }
            catch
            {
                Operation = "Save Failed";
            }
            finally
            {
                Enabled = true;
            }
        }

        protected override void OnClose()
        {
            proxy.Dispose();
            databaseSubscription.Dispose();
            dirtySubscription.Dispose();
            isModified.Dispose();
            base.OnClose();
        }

        protected override bool AskCloseConfirmation(ClosePageContext context)
        {
            string message = $"Document <{Document.DocumentId}> is not saved.\nClose it anyway?";
            var buttons = context.IsBatch ? DialogButtons.Yes | DialogButtons.YesToAll | DialogButtons.No | DialogButtons.NoToAll : DialogButtons.Yes | DialogButtons.No;
            var result = this.context.DialogService.ShowMessageBox(message, "Close confirmation", buttons, DialogButtons.Yes, DialogIcon.Question);
            switch (result)
            {
                case DialogButtons.Yes:
                    return true;

                case DialogButtons.YesToAll:
                    context.DirtyAction = CloseDirtyPageAction.ForceClose;
                    return true;

                case DialogButtons.NoToAll:
                case DialogButtons.Close:
                    context.DirtyAction = CloseDirtyPageAction.Keep;
                    return false;

                default:
                    return false;
            }
        }

        private void SchemaChanged(SchemafulDatabase dbView)
        {
            if (!Notifications.Items.OfType<SchemaChangedNotification>().Any())
            {
                if (IsDirty && context.AskSchemaUpdateConfirmationForModified.Value)
                    Notifications.Show(new SchemaChangedNotification(ApplySchema));
                else
                    ApplySchema();
            }
        }

        private void ApplySchema()
        {
            Notifications.RemoveAll<ConvertCategoryNotification>();
            SchemafulDatabase = context.ObservableSchemafulDatabase.Value;
            var draftJson = FormTab.DraftJson;
            SchemafulDatabase.GetDocumentSchema(Document, draftJson, out var newCategory, out var newSchema);
            Setup(draftJson, newCategory, newSchema);
        }

        public void UserDeleting()
        {
            Operation = "Deleting";
            Enabled = false;
        }

        public void UserDeleted()
        {
            Close(CloseDirtyPageAction.ForceClose);
        }

        public void RemoteDeleted(DocumentDraft draft)
        {
            Status = "Deleted";
            Operation = string.Empty;
            Notifications.RemoveAll<DocumentDeletedNotification>();
            Notifications.RemoveAll<DocumentChangedNotification>();
            Notifications.Show(new DocumentDeletedNotification(() => Close(CloseDirtyPageAction.ForceClose), () => EditAsNew(draft)));
        }

        public void UserDeleteFailed()
        {
            Operation = "Delete Failed";
            Enabled = true;
        }

        public void RemoteUpdated(DocumentRevision revision)
        {
            Notifications.RemoveAll<DocumentDeletedNotification>();
            Notifications.RemoveAll<DocumentChangedNotification>();
            Notifications.Show(new DocumentChangedNotification(() => TakeMine(revision), () => TakeRemote(revision)));
        }

        private void ModifiedChanged(bool isModified)
        {
            IsDirty = isModified;
            if (IsDirty)
            {
                if (Document.IsExisting)
                    Status = "Modified";
            }
            else
            {
                if (Status == "Modified")
                    Status = string.Empty;
            }
        }

        private void EditAsNew(DocumentDraft draft)
        {
            originalIsDirty.Value = true;
            Attachments.Rebase(draft.Attachments);
            Status = "New";
            Enabled = true;
        }

        private void TakeMine(DocumentRevision remoteRevision)
        {
            OriginalData = SanitizeData(remoteRevision.Json);
            FormTab.Form.SetOriginalJson(OriginalData);
            PatchHandler.UpdateRemote(remoteRevision.Json, false);
        }

        private void TakeRemote(DocumentRevision remoteRevision)
        {
            OriginalData = SanitizeData(remoteRevision.Json);
            FormTab.Form.SetOriginalJson(OriginalData);
            UpdateData(OriginalData);
            PatchHandler.UpdateRemote(remoteRevision.Json, true);
            Attachments.Setup(remoteRevision.Attachments);
            Status = string.Empty;
        }

        private static ImmutableJsonObject SanitizeData(ImmutableJsonObject json)
        {
            var result = new JsonObject(json);
            MetadataHelper.RemoveMetadata(result);
            result.Remove("_id");
            result.Remove("_rev");
            result.Remove("_attachments");
            result.Remove(CouchUtils.HerculesBase);
            return result;
        }

        private void Setup(ImmutableJsonObject json, Category category, SchemaRecord schema)
        {
            Category = category;
            Schema = schema;
            var sanJson = SanitizeData(json);
            FormTab.Form.Setup(sanJson, OriginalData, schema);
            if (ActiveTab == SourceTab)
                SourceTab.SetJson(sanJson);
            RaisePropertyChanged(nameof(IsScript));
            Broadcast<IOnApplySchema>(service => service.OnApplySchema(SchemafulDatabase, category, schema));
        }

        private void SetCategory(string category)
        {
            var data = new JsonObject(FormTab.DraftJson)
            {
                [SchemafulDatabase.Schema.Variant!.Tag] = category
            };
            UpdateData(data);
        }

        private void ConvertCategory()
        {
            if (!Notifications.Items.OfType<ConvertCategoryNotification>().Any())
                Notifications.Show(new ConvertCategoryNotification(SchemafulDatabase.Categories, cat => SetCategory(cat.Name)));
        }

        private async Task ValidateAndSave()
        {
            var validationResult = FormTab.GetIssues();

            if (validationResult.Count == 0)
                await Save().ConfigureAwait(true);
            else
                context.DialogService.ShowError(string.Join(Environment.NewLine, validationResult));
        }

        public void SummaryTable(JsonPath? path)
        {
            if (Category.IsSchemaful)
                context.SummaryTableAction(Category, path);
        }

        private void RunScript()
        {
            context.RunScriptAction(FormTab.DraftJson.AsObject["script"].AsString, null, new Progress<string>(), CancellationToken.None).Track();
        }
    }
}