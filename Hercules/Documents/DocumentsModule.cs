using Hercules.DB;
using Hercules.Documents.Dialogs;
using Hercules.Documents.Editor;
using Hercules.Forms;
using Hercules.Forms.Elements;
using Hercules.Forms.Schema;
using Hercules.Scripting;
using Hercules.Shell;
using Hercules.Summary;
using Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Hercules.Documents
{
    public readonly record struct EditDocumentRequest(IDocument Document, ImmutableJsonObject? Json = null, JsonPath? Path = null);

    public class DocumentsModule : CoreModule
    {
        public DocumentsModule(Core core)
            : base(core)
        {
            TextSizeService = new TextSizeService(new Typeface("Arial"), 11.5, Workspace.Dpi.PixelsPerDip);
            FormSchemaFactory = new FormSchemaFactory(FormSettings, TextSizeService, null, Workspace.DialogService, Workspace.ShortcutService, CustomTypeRegistry);
            metaSchema = AsyncValue.Create(LoadMetaSchema);

            void CreateDocumentAction(string documentId, DocumentDraft draft) => CreateDocument(documentId, draft);
            void CreateSchemalessDocumentAction(string documentId) => CreateDocument(documentId, new DocumentDraft(ImmutableJsonObject.Empty));
            void CloneDocumentAction(IDocument document, string newDocumentId) => CloneDocument(document, newDocumentId);

            this.EditDocumentCommand = Commands.Execute<IDocument>(doc => EditDocument(doc.DocumentId)).IfNotNull().AsBulk(EditDocuments);
            this.NewDocumentCommand = Commands.Execute<Category>(cat => Workspace.DialogService.ShowDialog(new NewDocumentDialog(Core.Project!.SchemafulDatabase, CreateDocumentAction, cat))).If(_ => Core.Project?.SchemafulDatabase?.Schema != null);
            this.NewSchemalessDocumentCommand = Commands.Execute(() => Workspace.DialogService.ShowDialog(new NewSchemalessDocumentDialog("New Schemaless Document", Core.Project!.SchemafulDatabase, CreateSchemalessDocumentAction))).If(HasDatabase);
            this.NewCustomEditorCommand = Commands.Execute(CreateCustomEditor).If(HasDatabase);
            this.CreateSchemaCommand = Commands.Execute(CreateSchema).If(() => Core.Project != null && Core.Project.SchemafulDatabase.Schema == null);
            this.RenameDocumentCommand = Commands.Execute<IDocument>(doc => Workspace.DialogService.ShowDialog(new RenameDocumentDialog(Core.Project!.SchemafulDatabase, this, doc))).IfNotNull();
            this.CloneDocumentCommand = Commands.Execute<IDocument>(doc => Workspace.DialogService.ShowDialog(new CloneDocumentDialog(Core.Project!.SchemafulDatabase, CloneDocumentAction, doc))).IfNotNull();
            this.MultiCloneDocumentCommand = Commands.Execute<IDocument>(doc => Workspace.DialogService.ShowDialog(new MultiCloneDocumentDialog(Core.Project!.SchemafulDatabase, CloneDocumentAction, doc))).IfNotNull();
            this.InheritDocumentCommand = Commands.Execute<IDocument>(doc => Workspace.DialogService.ShowDialog(new InheritDocumentDialog(Core.Project!.SchemafulDatabase, InheriteDocument, doc))).IfNotNull();
            this.DeleteDocumentCommand = Commands.Bulk<IDocument>(doc => DeleteDocumentAsync(doc, true).Track(), docs => DeleteDocuments(docs));
            this.CopyFauxtonUrlCommand = Commands.Execute<IDocument>(CopyFauxtonUrl).IfNotNull();
            this.CopyHerculesUrlCommand = Commands.Execute<IDocument>(CopyHerculesUrl).IfNotNull();
            this.CopyDocumentNameCommand = Commands.Execute<IDocument>(CopyDocumentName).IfNotNull();
            this.EditInFutonCommand = Commands.Execute<IDocument>(EditInFuton).IfNotNull();

            Workspace.ShortcutService.RegisterHandler(new DocumentShortcutHandler(Core.ProjectObservable, EditDocumentCommand.Single));
            Workspace.ShortcutService.RegisterHandler(new CategoryShortcutHandler(Core.ProjectObservable));

            settingsPageSubscription = core.Workspace.WindowService.WhenAddingPage.OfType<SettingsPage>().Subscribe(OnSettingsPageAdded);

            CustomTypeRegistry.Register(new IconCustomTypeSupport(EditorsMetaSchema, Workspace.DialogService));
            CustomTypeRegistry.Register(new BreadcrumbsCustomTypeSupport(EditorsMetaSchema));

            core.SettingsService.AddSetting(SchemaUpdate);
            core.SettingsService.AddSetting(AskSchemaUpdateConfirmationForModified);
            core.SettingsService.AddSettingGroup(FormSettings);

            SetupOptions(Workspace.OptionManager);
        }

        private readonly IDisposable settingsPageSubscription;

        private readonly Lazy<(FormSchema form, FormSchema diagram, FormSchema editors, FormSchema scripts)> metaSchema;

        public FormSchema FormMetaSchema => metaSchema.Value.form;
        public FormSchema DiagramMetaSchema => metaSchema.Value.diagram;
        public FormSchema EditorsMetaSchema => metaSchema.Value.editors;
        public FormSchema ScriptsMetaSchema => metaSchema.Value.scripts;
        public IMetaSchemaProvider MetaSchemaProvider => metaSchemaProvider ??= new MetaSchemaProvider(FormMetaSchema, DiagramMetaSchema, CustomTypeRegistry, ScriptsMetaSchema);
        private MetaSchemaProvider? metaSchemaProvider;

        public TextSizeService TextSizeService { get; }
        public FormSchemaFactory FormSchemaFactory { get; }
        public CustomTypeRegistry CustomTypeRegistry { get; } = new();

        public static readonly RoutedUICommand SaveDocument =
            new RoutedUICommand("Save Document", "SaveDocument", typeof(RoutedCommands), new InputGestureCollection { new KeyGesture(Key.S, ModifierKeys.Control) });

        public Setting<SchemaUpdateType> SchemaUpdate { get; } = new Setting<SchemaUpdateType>(nameof(SchemaUpdate), SchemaUpdateType.Silent);

        public Setting<bool> AskSchemaUpdateConfirmationForModified { get; } = new Setting<bool>(nameof(AskSchemaUpdateConfirmationForModified), false);

        public FormSettings FormSettings { get; } = new FormSettings();

        public ICommand<Category> NewDocumentCommand { get; }
        public ICommand NewSchemalessDocumentCommand { get; }
        public ICommand CreateSchemaCommand { get; }
        public ICommand NewCustomEditorCommand { get; }
        public IBulkCommand<IDocument> EditDocumentCommand { get; }
        public ICommand<IDocument> RenameDocumentCommand { get; }
        public ICommand<IDocument> CloneDocumentCommand { get; }
        public ICommand<IDocument> MultiCloneDocumentCommand { get; }
        public ICommand<IDocument> InheritDocumentCommand { get; }
        public IBulkCommand<IDocument> DeleteDocumentCommand { get; }
        public ICommand<IDocument> CopyFauxtonUrlCommand { get; }
        public ICommand<IDocument> CopyHerculesUrlCommand { get; }
        public ICommand<IDocument> CopyDocumentNameCommand { get; }
        public ICommand<IDocument> EditInFutonCommand { get; }

        private (FormSchema form, FormSchema diagram, FormSchema editors, FormSchema scripts) LoadMetaSchema()
        {
            var factory = new FormSchemaFactory(FormSettings, TextSizeService, null, Workspace.DialogService, Workspace.ShortcutService, CustomTypeRegistry);
            var form = factory.CreateFormSchema(LoadJsonFromResource("Hercules.Resources.Schema.schema.json"), null);
            var diagram = factory.CreateFormSchema(LoadJsonFromResource("Hercules.Resources.Schema.diagram_schema.json"), null);
            var editors = factory.CreateFormSchema(LoadJsonFromResource("Hercules.Resources.Schema.editors.json"), null);
            var scripts = factory.CreateFormSchema(LoadJsonFromResource("Hercules.Resources.Schema.scripts.json"), null);
            return (form, diagram, editors, scripts);
        }

        private void SetupOptions(UiOptionManager optionManager)
        {
            var newDocumentGesture = new KeyGesture(Key.N, ModifierKeys.Control);
            var newDocumentOption = new UiCommandOption("New Document...", Fugue.Icons.DocumentPlus, NewDocumentCommand.ForContext(Workspace), newDocumentGesture);
            optionManager.AddMenuOption(newDocumentOption, "Document#0/New#0", showInToolbar: true);
            optionManager.AddContextMenuOption<Category>(newDocumentOption);
            Workspace.AddGesture(newDocumentOption);

            var editDocumentOption = new UiCommandOption("Edit Document", Fugue.Icons.DocumentPencil, EditDocumentCommand.ForContext(Workspace));
            optionManager.AddContextMenuOption<IDocument>(editDocumentOption);

            var openDocumentGesture = new KeyGesture(Key.O, ModifierKeys.Control);
            var openDocumentCommand = Commands.Execute(() => Workspace.DialogService.ShowDialog(new OpenDocumentDialog(Core.Project!.SchemafulDatabase, documentId => EditDocument(documentId)))).If(HasDatabase);
            var openDocumentOption = new UiCommandOption("Open Document...", null, openDocumentCommand, openDocumentGesture);
            optionManager.AddMenuOption(openDocumentOption, "Document#0");
            Workspace.AddGesture(openDocumentOption);

            var saveDocumentOption = new UiCommandOption("Save Document", Fugue.Icons.Disk, SaveDocument);
            optionManager.AddMenuOption(saveDocumentOption, "Document#0", showInToolbar: true);

            var saveAllGesture = new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift);
            var saveAllCommand = Commands.Execute(SaveAll).If(() => Workspace.WindowService.Pages.Any(d => d.IsDirty));
            var saveAllOption = new UiCommandOption("Save All", Fugue.Icons.Disks, saveAllCommand, saveAllGesture);
            optionManager.AddMenuOption(saveAllOption, "Document#0", showInToolbar: true);
            Workspace.AddGesture(saveAllOption);

            var cloneGesture = new KeyGesture(Key.F5);
            var cloneDocumentOption = new UiCommandOption("Clone Document", Fugue.Icons.DocumentArrow, CloneDocumentCommand.ForContext(Workspace), cloneGesture);
            optionManager.AddMenuOption(cloneDocumentOption, "Document#0", showInToolbar: true);
            optionManager.AddContextMenuOption<IDocument>(cloneDocumentOption);
            Workspace.AddGesture(cloneDocumentOption);

            var multiCloneDocumentOption = new UiCommandOption("Multi Clone Document", null, MultiCloneDocumentCommand.ForContext(Workspace));
            optionManager.AddMenuOption(multiCloneDocumentOption, "Document#0");
            optionManager.AddContextMenuOption<IDocument>(multiCloneDocumentOption);

            var documentInheritanceOption = new UiCommandOption("Inherit Document", Fugue.Icons.DocumentLink, InheritDocumentCommand.ForContext(Workspace));
            optionManager.AddMenuOption(documentInheritanceOption, "Document#0", showInToolbar: true);
            optionManager.AddContextMenuOption<IDocument>(documentInheritanceOption);

            var renameDocumentGesture = new KeyGesture(Key.F2);
            var renameDocumentOption = new UiCommandOption("Rename Document", null, RenameDocumentCommand.ForContext(Workspace), renameDocumentGesture);
            optionManager.AddMenuOption(renameDocumentOption, "Document#0");
            optionManager.AddContextMenuOption<IDocument>(renameDocumentOption);
            Workspace.AddGesture(renameDocumentOption);

            var deleteDocumentOption = new UiCommandOption("Delete Document", Fugue.Icons.DocumentMinus, DeleteDocumentCommand.ForContext(Workspace));
            optionManager.AddMenuOption(deleteDocumentOption, "Document#0", showInToolbar: true);
            optionManager.AddContextMenuOption<IDocument>(deleteDocumentOption);

            var editInFutonOption = new UiCommandOption("Edit In Fauxton...", Fugue.Icons.DocumentGlobe, EditInFutonCommand.ForContext(Workspace));
            optionManager.AddMenuOption(editInFutonOption, "Document#20", showInToolbar: true);
            optionManager.AddContextMenuOption<IDocument>(editInFutonOption);

            var copyDocumentNameOption = new UiCommandOption("Copy Document Name", null, CopyDocumentNameCommand.ForContext(Workspace));
            optionManager.AddMenuOption(copyDocumentNameOption, "Document#30");
            optionManager.AddContextMenuOption<IDocument>(copyDocumentNameOption);

            var copyFauxtonUrlOption = new UiCommandOption("Copy Fauxton URL", null, CopyFauxtonUrlCommand.ForContext(Workspace));
            optionManager.AddMenuOption(copyFauxtonUrlOption, "Document#30");

            var copyHerculesUrlOption = new UiCommandOption("Copy Hercules URL", null, CopyHerculesUrlCommand.ForContext(Workspace));
            optionManager.AddMenuOption(copyHerculesUrlOption, "Document#30");

            var newSchemalessDocumentOption = new UiCommandOption("New Schemaless Document...", null, NewSchemalessDocumentCommand);
            optionManager.AddMenuOption(newSchemalessDocumentOption, "Document#0/New#0");

            var newCustomEditorOption = new UiCommandOption("New Custom Editor...", null, NewCustomEditorCommand);
            optionManager.AddMenuOption(newCustomEditorOption, "Document#0/New#0");

            var createSchemaOption = new UiCommandOption("Create Schema...", null, CreateSchemaCommand);
            optionManager.AddMenuOption(createSchemaOption, "Document#0/New#20");

            var findInvalidDocumentsCommand = Commands.Execute(FindInvalid).If(HasDatabase);
            var findInvalidDocumentsOption = new UiCommandOption("Find Invalid Documents", Fugue.Icons.DocumentSmileySad, findInvalidDocumentsCommand);
            optionManager.AddMenuOption(findInvalidDocumentsOption, "Data#0");

            var compareDocumentsCommand = Commands.Execute(() => Workspace.WindowService.OpenPage(new CompareDocumentsPage(Core.Project!.Database, null, null))).If(HasDatabase);
            var compareDocumentsOption = new UiCommandOption("Compare Documents", Fugue.Icons.DocumentView, compareDocumentsCommand);
            optionManager.AddMenuOption(compareDocumentsOption, "Data#0", showInToolbar: true);
        }

        public void OnSettingsPageAdded(SettingsPage page)
        {
            page.Tabs.Add(new DocumentEditorSettingsTab(FormSettings));
            page.Tabs.Add(new SchemaUpdateSettingsTab(SchemaUpdate, AskSchemaUpdateConfirmationForModified));
        }

        private void SaveAll()
        {
            var invalidDocuments = new List<string>();

            foreach (var doc in Workspace.WindowService.Pages.OfType<DocumentEditorPage>().Where(d => d.IsDirty))
            {
                var validationResult = doc.FormTab.GetErrorList();

                if (validationResult.Count != 0)
                    invalidDocuments.Add(doc.Document.DocumentId);
                else
                    doc.Save().Track();
            }

            if (invalidDocuments.Count > 0)
                Workspace.DialogService.ShowError("Invalid documents were not saved:\r\n\r\n" + string.Join("\r\n", invalidDocuments));
        }

        private void FindInvalid()
        {
            var now = DateTime.Now;
            var elementFactory = new ElementFactory(CustomTypeRegistry, ElementFactoryContext.Default);

            bool IsInvalidDocument(IDocument doc)
            {
                var schemaRecord = Core.Project.SchemafulDatabase.Schema.Variant.GetChildForJson(doc.Json);
                var form = new DocumentForm(doc.Json, doc.Json, schemaRecord, elementFactory, FormSettings);
                return !form.Root.IsValid || form.IsModified.Value;
            }

            var invalidDocs = Core.Project!.SchemafulDatabase.SchemafulDocuments.Where(IsInvalidDocument).ToList();
            Logger.LogDebug("Find invalid documents completed in " + (DateTime.Now - now).TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture));
            if (invalidDocs.Any())
            {
                foreach (var doc in invalidDocs)
                {
                    EditDocument(doc.DocumentId);
                }
            }
            else
            {
                Workspace.DialogService.ShowMessageBox("No invalid documents found", "Find Invalid Documents", DialogButtons.Ok, DialogButtons.Ok, DialogIcon.Smile);
            }
        }

        private unsafe ImmutableJson LoadJsonFromResource(string path)
        {
            using var stream = (UnmanagedMemoryStream)GetType().Assembly.GetManifestResourceStream(path)!;
            var span = new ReadOnlySpan<byte>(stream.PositionPointer, checked((int)stream.Length));
            var reader = new Utf8JsonReader(span);
            return Utf8Json.Parse(ref reader);
        }

        private bool HasDatabase() => Core.Project != null;

        private DocumentEditorContext GetDocumentEditorContext()
        {
            return new DocumentEditorContext(
                GetDocumentProxy: (document, editor) => new DocumentProxy(Core.Project.Database, Core.Project.Connection.Username, document, editor),
                ObservableSchemafulDatabase: Core.Project.ObservableSchemafulDatabase,
                DialogService: Workspace.DialogService,
                GetElementFactory: elementFactoryContext => new ElementFactory(CustomTypeRegistry, elementFactoryContext),
                FormSettings: FormSettings,
                AskSchemaUpdateConfirmationForModified: AskSchemaUpdateConfirmationForModified,
                TempStorage: Core.Project.Database.TempStorage,
                SummaryTableAction: Core.GetModule<SummaryModule>().SummaryTable,
                RunScriptAction: Core.GetModule<ScriptingModule>().RunScriptAsync);
        }

        public DocumentEditorPage EditDocument(EditDocumentRequest request)
        {
            return EditDocument(request.Document.DocumentId, request.Json, request.Path);
        }

        public DocumentEditorPage EditDocument(string docName, ImmutableJsonObject? json = null, JsonPath? path = null)
        {
            if (Core.Project == null)
                throw new InvalidOperationException("No active project");
            if (!Core.Project.Database.Documents.TryGetValue(docName, out var document))
                throw new ArgumentException($"Document {docName} not found");
            var editor = Workspace.WindowService.Pages.OfType<DocumentEditorPage>().FirstOrDefault(doc => doc.Document.DocumentId == document.DocumentId);
            if (editor == null)
            {
                editor = new DocumentEditorPage(GetDocumentEditorContext(), document);
                Workspace.WindowService.AddPage(editor);
            }
            if (json != null)
            {
                editor.UpdateData(json);
                editor.GoTo(DestinationTab.Form);
            }
            if (Workspace.WindowService.ActiveContent == null || (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.None)
                Workspace.WindowService.ActiveContent = editor;
            if (path != null)
                editor.GoTo(DestinationTab.Form, path);
            return editor;
        }

        public void EditDocuments(IReadOnlyDictionary<string, ImmutableJsonObject> pairs)
        {
            if (Core.Project == null)
                throw new InvalidOperationException("No active project");

            var newPages = new List<DocumentEditorPage>();
            var context = GetDocumentEditorContext();
            foreach (var pair in pairs)
            {
                Core.Project.Database.Documents.TryGetValue(pair.Key, out var document);
                var editor = Workspace.WindowService.Pages.OfType<DocumentEditorPage>().FirstOrDefault(doc => doc.Document.DocumentId == document.DocumentId);
                if (editor == null)
                {
                    editor = new DocumentEditorPage(context, document);
                    newPages.Add(editor);
                }
                editor.UpdateData(pair.Value);
                editor.GoTo(DestinationTab.Form);
            }
            Workspace.WindowService.AddPages(newPages);
        }

        public void EditDocuments(IReadOnlyList<EditDocumentRequest> requests)
        {
            if (Core.Project == null)
                throw new InvalidOperationException("No active project");

            var newPages = new List<DocumentEditorPage>();
            var context = GetDocumentEditorContext();
            foreach (var request in requests)
            {
                var editor = Workspace.WindowService.Pages.OfType<DocumentEditorPage>().FirstOrDefault(doc => doc.Document.DocumentId == request.Document.DocumentId);
                if (editor == null)
                {
                    editor = new DocumentEditorPage(context, request.Document);
                    newPages.Add(editor);
                }
                if (request.Json != null)
                {
                    editor.UpdateData(request.Json);
                    editor.GoTo(DestinationTab.Form);
                }
                if (request.Path != null)
                    editor.GoTo(DestinationTab.Form, request.Path);
            }
            Workspace.WindowService.AddPages(newPages);
        }

        public void EditDocuments(IReadOnlyCollection<IDocument> documents)
        {
            if (Core.Project == null)
                throw new InvalidOperationException("No active project");

            var newPages = new List<DocumentEditorPage>();
            var context = GetDocumentEditorContext();
            foreach (var document in documents)
            {
                if (document.Editor == null)
                {
                    var editor = new DocumentEditorPage(context, document);
                    newPages.Add(editor);
                }
            }
            Workspace.WindowService.AddPages(newPages);
        }

        public DocumentEditorPage CreateDocument(string docName, DocumentDraft draft)
        {
            if (Core.Project == null)
                throw new InvalidOperationException("No active project");

            Core.Project.Database.CreateDocument(docName, draft);
            return EditDocument(docName);
        }

        public DocumentEditorPage CloneDocument(IDocument source, string newDocName)
        {
            if (source.IsExisting)
            {
                return CreateDocument(newDocName, new DocumentDraft(source.CurrentRevision.Json, source.CurrentRevision.Attachments.Select(a => AttachmentDraft.CopyFrom(a, Core.Project.Database.TempStorage)).ToList()));
            }
            else
            {
                var draft = source.Editor != null ? source.Editor.GetDocumentDraft() : source.Draft;
                return CreateDocument(newDocName, draft);
            }
        }

        public Task SaveDocumentAsync(IDocument document, DocumentDraft draft)
        {
            if (Core.Project == null)
                throw new InvalidOperationException("No active project");

            var metadata = new MetadataDraft(Core.Project.Connection.Username);
            return Core.Project.Database.SaveDocumentAsync(document, draft, metadata);
        }

        public void InheriteDocument(IDocument source, string documentName)
        {
            var docAttachments = source.CurrentRevision.Attachments.Select(a => AttachmentDraft.CopyFrom(a, Core.Project.Database.TempStorage)).ToList();
            var json = new JsonObject(source.Json)
            {
                [CouchUtils.HerculesBase] = source.CurrentRevision.Json.WithoutKeys(CouchUtils.BaseJsonExcludedKeys)
            };

            var docDraft = new DocumentDraft(json, docAttachments);
            CreateDocument(documentName, docDraft);
        }

        public async Task DeleteDocumentAsync(IDocument document, bool askConfirmation)
        {
            if (Core.Project == null)
                throw new InvalidOperationException("No active project");

            if (askConfirmation && !Workspace.DialogService.ShowQuestion($"Are you sure you want to delete document <{document.DocumentId}>?"))
                return;

            if (document.IsExisting)
                await Core.Project.Database.DeleteDocumentAsync(document).ConfigureAwait(true);
            Workspace.WindowService.Pages.OfType<DocumentEditorPage>().FirstOrDefault(doc => doc.Document.DocumentId == document.DocumentId)?.Close(CloseDirtyPageAction.ForceClose);
        }

        public bool DeleteDocuments(IReadOnlyCollection<IDocument> documents)
        {
            if (Core.Project == null)
                throw new InvalidOperationException("No project");

            return DeleteDocuments(Core.Workspace, Core.Project.Database, documents);
        }

        public static bool DeleteDocuments(Workspace workspace, Database database, IReadOnlyCollection<IDocument> documents)
        {
            ArgumentNullException.ThrowIfNull(database);

            if (documents.Count == 0)
                return true;

            static bool ConfirmDeleteDocuments(Workspace workspace, IReadOnlyCollection<IDocument> documents)
            {
                string docList;
                if (documents.Count <= 5)
                    docList = string.Join(Environment.NewLine, documents.Select(doc => doc.DocumentId));
                else
                    docList = string.Join(Environment.NewLine, documents.Take(3).Select(doc => doc.DocumentId)) + Environment.NewLine + "and " + (documents.Count - 3) + " more documents";
                return workspace.DialogService.ShowQuestion("Are you sure you want to delete documents: " + Environment.NewLine + docList + "?");
            }

            if (!ConfirmDeleteDocuments(workspace, documents))
                return false;

            foreach (var document in documents)
            {
                if (document.IsExisting)
                    database.DeleteDocumentAsync(document).Track();
                workspace.WindowService.Pages.OfType<DocumentEditorPage>().FirstOrDefault(doc => doc.Document == document)?.Close(CloseDirtyPageAction.ForceClose);
            }
            return true;
        }

        private void CreateCustomEditor()
        {
            var dialog = new NewEditorDocumentDialog(Core.Project!.SchemafulDatabase, CustomTypeRegistry.All);
            if (Workspace.DialogService.ShowDialog(dialog))
            {
                var draft = dialog.CustomType.CreateEditorDraft(dialog.DocumentName, Core.Project.Database.TempStorage);
                var doc = Core.Project.Database.CreateDocument(dialog.DocumentName, draft);
                EditDocument(doc.DocumentId);
            }
        }

        private void CreateSchema()
        {
            var schema = Igor.Schema.SchemaJsonSerializer.Instance.Serialize(new Igor.Schema.Schema(new Dictionary<string, Igor.Schema.CustomType>(), ""));
            var schemaJson = new JsonObject(schema.AsObject) { ["scope"] = "schema" };
            CreateDocument("schema", new DocumentDraft(schemaJson));
        }

        private void CopyFauxtonUrl(IDocument document)
        {
            var url = Fauxton.GetUrl(Core.Project!.Connection, document.DocumentId, false);
            Clipboard.SetText(url.ToString());
        }

        private void CopyHerculesUrl(IDocument document)
        {
            var url = HerculesUrl.GetUrl(Core.Project!.Connection, document);
            Clipboard.SetText(url);
        }

        private void CopyDocumentName(IDocument document)
        {
            Clipboard.SetText(document.DocumentId);
        }

        private void EditInFuton(IDocument document)
        {
            var url = Fauxton.GetUrl(Core.Project!.Connection, document.DocumentId, false);
            Workspace.OpenExternalBrowser(url);
        }
    }
}