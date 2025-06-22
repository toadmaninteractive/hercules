using Hercules.Connections;
using Hercules.DB;
using Hercules.Documents;
using Hercules.Repository;
using Hercules.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Hercules
{
    public class Core : NotifyPropertyChanged
    {
        public Core(Workspace workspace, SettingsService settingsService, bool isBatch)
        {
            Workspace = workspace;
            SettingsService = settingsService;
            IsBatch = isBatch;
            databaseSingletonService = new DatabaseSingletonService(workspace);
        }

        public bool IsBatch { get; }
        public Workspace Workspace { get; }

        public SettingsService SettingsService { get; }

        private readonly List<CoreModule> modules = new();
        public IReadOnlyList<CoreModule> Modules => modules;

        public T GetModule<T>() where T : CoreModule
        {
            return Modules.OfType<T>().First();
        }

        public void AddModule(CoreModule module)
        {
            modules.Add(module);
        }

        private readonly ObservableValue<Project?> projectObservable = new ObservableValue<Project?>(null);
        private readonly IDatabaseSingletonService databaseSingletonService;
        private IDisposable databaseSingleton;

        public Project? Project
        {
            get => projectObservable.Value;
            private set
            {
                if (projectObservable.Value != value)
                {
                    projectObservable.Value = value;
                    RaisePropertyChanged(nameof(Project));
                }
            }
        }

        public IReadOnlyObservableValue<Project?> ProjectObservable => projectObservable;

        public void OnLoad(Uri? startUri)
        {
            modules.ForEach(mod => mod.OnLoad(startUri));
            modules.ForEach(mod => mod.OnLoaded());
            if (startUri != null && Workspace.ShortcutService.TryParseUri(startUri, out var shortcut))
                Workspace.ShortcutService.Open(shortcut);
            var startTime = (DateTime.Now - Process.GetCurrentProcess().StartTime).TotalSeconds;
            Logger.LogDebug($"Application startup time {startTime.ToString("0.00", CultureInfo.InvariantCulture)}s");
        }

        public void OpenProject(DbConnection connection, Database database)
        {
            var documentsModule = GetModule<DocumentsModule>();
            var settingsPath = Path.Combine(connection.Path, "user.json");
            ISettingsReader settingsReader;
            if (File.Exists(settingsPath))
                settingsReader = new JsonSettingsReader(settingsPath);
            else
                settingsReader = EmptySettingsReader.Default;

            var schemaUpdater = new SchemaUpdater(Workspace, documentsModule.SchemaUpdate);
            var projectSettings = new ProjectSettings(Workspace.DialogService);
            var formSchemaFactory = documentsModule.FormSchemaFactory with { ProjectSettings = projectSettings };
            projectSettings.Load(settingsReader);
            databaseSingleton = databaseSingletonService.CreateIpcListener(HerculesUrl.GetDatabaseUrl(connection));
            var newProject = new Project(connection, database, projectSettings, schemaUpdater, formSchemaFactory, documentsModule.MetaSchemaProvider);
            modules.ForEach(mod => mod.OnLoadProject(newProject, settingsReader));
            Project = newProject;
        }

        public void CloseProject()
        {
            if (Project != null)
            {
                databaseSingleton?.Dispose();
                databaseSingleton = null;
                Workspace.WindowService.CloseAllPages(CloseDirtyPageAction.ForceClose);
                SaveProject();
                Project.Close();
                modules.ForEach(mod => mod.OnCloseProject());
                Project = null;
            }
        }

        public void SaveProject()
        {
            if (Project != null)
            {
                var settingsWriter = new JsonSettingsWriter();
                Project.Settings.Save(settingsWriter);
                modules.ForEach(mod => mod.OnSaveProject(settingsWriter));
                settingsWriter.Save(Path.Combine(Project.Connection.Path, "user.json"));
            }
        }

        public void Shutdown()
        {
            modules.ForEach(mod => mod.OnShutdown());
        }

        public static Version GetVersion()
        {
            return Assembly.GetEntryAssembly()!.GetName().Version!;
        }

        public static int Revision => GetVersion().Revision;

        public static string? GetCliArgument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            var i = Array.IndexOf(args, name);
            return i > 0 && i < args.Length - 1 ? args[i + 1] : null;
        }

        public static bool HasCliArgument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            return Array.IndexOf(args, name) >= 0;
        }
    }
}
