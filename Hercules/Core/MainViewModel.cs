using AvalonDock;
using Hercules.Analysis;
using Hercules.ApplicationUpdate;
using Hercules.Bookmarks;
using Hercules.Connections;
using Hercules.DatabaseExplorer;
using Hercules.Diagrams;
using Hercules.Dialogs;
using Hercules.Documents;
using Hercules.History;
using Hercules.Import;
using Hercules.InteractiveMaps;
using Hercules.Plots;
using Hercules.Replication;
using Hercules.Scripting;
using Hercules.Search;
using Hercules.ServerBrowser;
using Hercules.Shell;
using Hercules.Shell.View;
using Hercules.Shortcuts;
using Hercules.Summary;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Hercules.Repository;
using Hercules.Localization;
using Hercules.Cards;

namespace Hercules
{
    public sealed class MainViewModel
    {
        public Core Core { get; }
        public Workspace Workspace { get; }
        public WindowService WindowService { get; }
        public DockingLayoutService DockingLayoutService { get; }
        public CommandBindingCollection RoutedCommandBindings { get; } = new();
        public InputBindingCollection InputBindings { get; } = new();
        public ConnectionsModule ConnectionsModule { get; }
        public BookmarksModule BookmarksModule { get; }

        public MainViewModel(Window mainWindow, DockingManager dockingManager)
        {
            Logger.Log("Application started...");
            Logger.Log($"Version: {Core.GetVersion()}");
            var args = Environment.GetCommandLineArgs();
            bool resetLayout = args.Contains("-reset_layout");
            bool isBatchMode = args.Contains("-batch");
            PathUtils.EnsureFoldersExist();
            var settingsFileName = Path.Combine(PathUtils.ConfigFolder, "settings.json");
            var settingsService = new SettingsService(settingsFileName);
            var scheduler = new BackgroundJobScheduler(mainWindow.Dispatcher);
            var dialogService = new DialogService(mainWindow);
            var shortcutService = new ShortcutService();
            var commandManager = new UiOptionManager();
            DockingLayoutService = new DockingLayoutService(dockingManager, Path.Combine(PathUtils.ConfigFolder, "layout.xml"), resetLayout);
            WindowService = new WindowService();
            var dpi = VisualTreeHelper.GetDpi(mainWindow);
            this.Workspace = new Workspace(scheduler, WindowService, DockingLayoutService, dialogService, commandManager, shortcutService, RoutedCommandBindings, InputBindings, dpi, () => BringToFront(mainWindow));
            settingsService.AddSettingGroup(Workspace);
            this.Core = new Core(Workspace, settingsService, isBatchMode);
            Core.AddModule(new ApplicationUpdateModule(Core));
            Core.AddModule(ConnectionsModule = new ConnectionsModule(Core));
            Core.AddModule(new RepositoryModule(Core));
            Core.AddModule(new DocumentsModule(Core));
            Core.AddModule(new SearchModule(Core));
            Core.AddModule(new AnalysisModule(Core));
            Core.AddModule(new SummaryModule(Core));
            Core.AddModule(new ScriptingModule(Core));
            Core.AddModule(new HistoryModule(Core));
            Core.AddModule(BookmarksModule = new BookmarksModule(Core));
            Core.AddModule(new DatabaseExplorerModule(Core));
            Core.AddModule(new LocalizationModule(Core));
            Core.AddModule(new ImportModule(Core));
            Core.AddModule(new ReplicationModule(Core));
            Core.AddModule(new DiagramModule(Core));
            Core.AddModule(new DialogModule(Core));
            Core.AddModule(new PlotsModule(Core));
            Core.AddModule(new ServerBrowserModule(Core));
            Core.AddModule(new InteractiveMapModule(Core));
            Core.AddModule(new CardsModule(Core));
            Workspace.BuildUI();
            if (File.Exists(settingsFileName))
            {
                settingsService.Load(new JsonSettingsReader(settingsFileName));
            }
            settingsService.Save();
            settingsService.SaveOnChange = true;
        }

        private static void BringToFront(Window window)
        {
            if (window.WindowState == WindowState.Minimized)
            {
                window.WindowState = WindowState.Normal;
            }

            window.Activate();
        }

        public void OnLoad()
        {
            var args = Environment.GetCommandLineArgs();
            Core.OnLoad(GetStartUri(args));

            if (Core.IsBatch)
            {
                logFile = GetArgument(args, "-log");
                if (logFile != null)
                {
                    File.Delete(logFile);
                    outputLogToConsoleSubscription = Logger.Events.Subscribe(OutputLogToConsole);
                }

                _ = RunBatchScriptAsync(args);
            }
        }

        private IDisposable? outputLogToConsoleSubscription;
        private string? logFile;

        private void OutputLogToConsole(LogEvent logEvent)
        {
            File.AppendAllText(logFile!, logEvent.Copy() + Environment.NewLine);
        }

        private async Task RunBatchScriptAsync(string[] args)
        {
            try
            {
                var filename = GetArgument(args, "-batch");
                if (filename != null)
                {
                    Logger.Log($"Executing {filename}");
                    string script;
                    if (filename.StartsWith("hercules:"))
                    {
                        script = Core.Project.Database.Documents[HerculesUrl.DocumentId(new Uri(filename))].Json.AsObject["script"].AsString;
                    }
                    else
                    {
                        script = await File.ReadAllTextAsync(filename);
                    }
                    await Core.GetModule<ScriptingModule>().RunScriptAsync(script, null, new Progress<string>(), default);
                }
                else
                {
                    Logger.Log($"No batch file provided");
                }

                Application.Current.Shutdown(0);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                Application.Current.Shutdown(1);
            }
        }

        private string? GetArgument(string[] args, string name)
        {
            var i = Array.IndexOf(args, name);
            return i > 0 && i < args.Length - 1 ? args[i + 1] : null;
        }

        private Uri? GetStartUri(string[] args)
        {
            string? startUriString;
            if (args.Length == 2 && !args[1].StartsWith("-"))
            {
                startUriString = args.Last();
            }
            else
            {
                startUriString = GetArgument(args, "-open") ?? GetArgument(args, "-dispatch");
                if (startUriString == null)
                {
                    var batchString = GetArgument(args, "-batch");
                    if (batchString != null && batchString.StartsWith("hercules:"))
                        startUriString = batchString;
                }
            }

            if (startUriString != null)
            {
                Logger.Log("Start URL: " + startUriString);
                return ParseStartUri(startUriString);
            }

            return null;
        }

        private Uri? ParseStartUri(string startUriString)
        {
            Uri? startUri = null;
            if (!string.IsNullOrEmpty(startUriString))
            {
                try
                {
                    startUri = new Uri(startUriString);
                }
                catch (UriFormatException ex)
                {
                    startUri = null;
                    Logger.LogException($"Failed to parse start URL: {startUriString}", ex);
                }
            }

            return startUri;
        }
    }
}
