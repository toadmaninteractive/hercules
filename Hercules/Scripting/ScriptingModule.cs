using Hercules.ApplicationUpdate;
using Hercules.Diagrams;
using Hercules.Documents;
using Hercules.Documents.Dialogs;
using Hercules.Documents.Editor;
using Hercules.Scripting.JavaScript;
using Hercules.Shell;
using Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Hercules.Scripting
{
    public class ScriptingModule : CoreModule
    {
        private readonly ScriptMenuService scriptMenuService;

        public ICommand<string> RunScriptDocumentCommand { get; }

        public string? LastScript { get; private set; }

        private readonly IDisposable documentPageSubscription;

        public ScriptingModule(Core core)
            : base(core)
        {
            documentPageSubscription = Workspace.WindowService.WhenAddingPage.OfType<DocumentEditorPage>().Subscribe(page => page.Services.Add(new ScriptingPageService(page, Core.Project, this)));

            var runScriptOption = new UiCommandOption("Run Script", Fugue.Icons.ScriptAttributeJ, Commands.Execute(() => OpenScript()).If(() => Core.Project != null));
            var scriptingReferenceOption = new UiCommandOption("Scripting Reference", null, ShowScriptingReference);
            Workspace.OptionManager.AddMenuOption(runScriptOption, "Data#0", showInToolbar: true);
            Workspace.OptionManager.AddMenuOption(scriptingReferenceOption, "Help#0");

            var newScriptCommand = Commands.Execute(() => Workspace.DialogService.ShowDialog(new NewSchemalessDocumentDialog("New Script Document", Core.Project!.SchemafulDatabase, CreateScriptDocument))).If(() => Core.Project != null);
            var newSchemalessDocumentOption = new UiCommandOption("New Script...", null, newScriptCommand);
            Workspace.OptionManager.AddMenuOption(newSchemalessDocumentOption, "Document#0/New#0");

            RunScriptDocumentCommand = Commands.Execute<string>(RunScriptDocument);
            scriptMenuService = new ScriptMenuService(Core.ProjectObservable, Workspace, RunScriptDocumentCommand);
        }

        private void RunScriptDocument(string documentId)
        {
            var doc = Core.Project.Database.Documents[documentId];
            var script = ScriptDocumentJsonSerializer.Instance.Deserialize(doc.Json);
            RunScriptAsync(script.Script, null, new Progress<string>(), default).Track();
        }

        void CreateScriptDocument(string documentId)
        {
            var script = new ScriptDocument { Script = "hercules.alert('Hello World');" };
            var draft = new DocumentDraft(ScriptDocumentJsonSerializer.Instance.Serialize(script).AsObject);
            Core.GetModule<DocumentsModule>().CreateDocument(documentId, draft);
        }

        void ShowScriptingReference()
        {
            var branch = Core.GetModule<ApplicationUpdateModule>().UpdateChannel.Value == ApplicationUpdateChannel.Stable ? "stable" : "latest";
            Workspace.OpenExternalBrowser(new Uri($"https://toadman-hercules.readthedocs.io/{branch}/scripting/scripting-index.html"));
        }

        public ScriptContext CreateScriptContext()
        {
            return new ScriptContext(Core, Dispatcher.CurrentDispatcher);
        }

        public void OpenScript(string? preset = null)
        {
            Workspace.WindowService.OpenPage(new DocumentScriptPage(Core.Project, this, Core.Workspace, preset));
        }

        public async Task RunScriptAsync(string script, IReadOnlyList<IDocument>? documents, IProgress<string> progress, CancellationToken cancellationToken)
        {
            var sw = new Stopwatch();
            sw.Start();
            var context = CreateScriptContext();
            var success = false;

            if (documents != null)
            {
                success = await Task.Run(() => RunDocumentScriptAsync(script, documents, context, progress, cancellationToken), cancellationToken).ConfigureAwait(true);
            }
            else
            {
                success = await Task.Run(() => RunDatabaseScriptAsync(script, context, progress, cancellationToken), cancellationToken).ConfigureAwait(true);
            }

            if (success)
            {
                progress.Report("Load modified documents...");
                await Task.Delay(10, cancellationToken).ConfigureAwait(true);
            }
            await context.FlushAsync();
            sw.Stop();
            Logger.LogDebug($"Script executed in {sw.ElapsedMilliseconds}ms");
        }

        private static bool RunDatabaseScriptAsync(string script, ScriptContext context, IProgress<string> progress, CancellationToken cancellationToken)
        {
            progress.Report("Compiling...");

            var errors = JsHost.SyntaxCheck(script);
            if (errors == null)
            {
                var host = new JsHost(context.ScriptingModuleProvider);
                host.SetValue("hercules", new HerculesJsApi(context, host).Api);
                host.Include(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "JavaScript\\utils.js"));
                cancellationToken.ThrowIfCancellationRequested();
                progress.Report("Processing...");
                try
                {
                    host.Execute(script);
                    return true;
                }
                catch (Exception e)
                {
                    Logger.LogException("Script error", e);
                    return false;
                }
            }
            else
            {
                foreach (var ex in errors)
                    Logger.LogException("Script error", ex);
                return false;
            }
        }

        private static bool RunDocumentScriptAsync(string script, IReadOnlyList<IDocument> docs, ScriptContext context, IProgress<string> progress, CancellationToken cancellationToken)
        {
            progress.Report("Compiling...");

            var errors = JsHost.SyntaxCheck(script);
            if (errors == null)
            {
                var host = new JsHost(context.ScriptingModuleProvider);
                host.SetValue("hercules", new HerculesJsApi(context, host).Api);
                host.Include(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "JavaScript\\utils.js"));
                foreach (var doc in docs)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    progress.Report("Processing " + doc.DocumentId);
                    if (!doc.IsDesign && doc.IsExisting)
                    {
                        try
                        {
                            var originalJson = context.ActiveDatabaseContext.GetJson(doc.DocumentId)!;
                            ImmutableJsonObject? result = null;
                            try
                            {
                                result = host.SetJsonValue("doc", originalJson).Execute(script).GetJsonValue("doc").AsObject;
                            }
                            catch (Exception e)
                            {
                                Logger.LogException("Script error", e);
                            }
                            if (result != null && !ImmutableJson.Equals(result, originalJson))
                            {
                                context.ActiveDatabaseContext.UpdateJson(doc.DocumentId, result);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogException("Script error", ex);
                        }
                    }
                }
                return true;
            }
            else
            {
                foreach (var ex in errors)
                    Logger.LogException("Script error", ex);
                return false;
            }
        }

        public override void OnLoadProject(Project project, ISettingsReader settingsReader)
        {
            if (settingsReader.Read<string>(nameof(LastScript), out var lastScript) && !string.IsNullOrWhiteSpace(lastScript))
                LastScript = lastScript;
        }

        public override void OnSaveProject(ISettingsWriter settingsWriter)
        {
            if (!string.IsNullOrWhiteSpace(LastScript))
                settingsWriter.Write(nameof(LastScript), LastScript ?? string.Empty);
        }

        public void SaveLastScript(string script)
        {
            LastScript = script;
            Core.SaveProject();
        }
    }
}
