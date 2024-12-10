using Hercules.Documents;
using Hercules.Shell;
using Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hercules.Scripting
{
    public class ScriptMenuService : IDisposable
    {
        private readonly IReadOnlyObservableValue<Project?> projectObservable;
        private readonly IDisposable projectSubscription;
        private readonly List<UiMenuOption> menuOptions = new();
        private readonly Workspace workspace;
        private readonly ICommand<string> runScriptDocumentCommand;
        private IDisposable? databaseSubscription;

        public ScriptMenuService(IReadOnlyObservableValue<Project?> projectObservable, Workspace workspace, ICommand<string> runScriptDocumentCommand)
        {
            this.projectObservable = projectObservable;
            this.workspace = workspace;
            this.runScriptDocumentCommand = runScriptDocumentCommand;
            projectSubscription = projectObservable.Subscribe(ProjectChanged);
        }

        public void ProjectChanged(Project? project)
        {
            databaseSubscription?.Dispose();
            databaseSubscription = null;
            RebuildUI(project);
            if (project != null)
                databaseSubscription = project.Database.Changes.Subscribe(DatabaseChanged);
        }

        void RebuildUI(Project? project)
        {
            bool needRebuild = UninstallUI();
            if (project != null)
                needRebuild = InstallUI(project.SchemafulDatabase) || needRebuild;
            if (needRebuild)
                workspace.BuildUI();
        }

        private void DatabaseChanged(DatabaseChange change)
        {
            if (CouchUtils.GetScope(change.Document.Json) == "script")
            {
                RebuildUI(projectObservable.Value);
            }
        }

        private bool InstallUI(SchemafulDatabase database)
        {
            bool needRebuild = false;
            foreach (var document in database.ScriptDocuments.Documents)
            {
                var script = JsonTypes.TryDeserialize(ScriptDocumentJsonSerializer.Instance, document.Json);
                if (script.HasValue && script.Value.Name != null && script.Value.MenuCategory != null)
                {
                    var uiOption = new UiCommandOption(script.Value.Name, script.Value.Icon, runScriptDocumentCommand.For(document.DocumentId));
                    menuOptions.Add(workspace.OptionManager.AddMenuOption(uiOption, script.Value.MenuCategory, script.Value.ShowInToolbar ?? false));
                    needRebuild = true;
                }
            }
            return needRebuild;
        }

        public bool UninstallUI()
        {
            var result = menuOptions.Any();
            foreach (var menuOption in menuOptions)
            {
                workspace.OptionManager.RemoveMenuOption(menuOption);
            }
            menuOptions.Clear();
            return result;
        }

        public void Dispose()
        {
            databaseSubscription?.Dispose();
            projectSubscription.Dispose();
        }
    }
}
