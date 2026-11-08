using Hercules.Shortcuts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Hercules.Shell
{
    public class WorkspaceSettings : ISettingGroup
    {
        public Setting<bool> LoadLastDatabaseOnStartup { get; } = new(nameof(LoadLastDatabaseOnStartup), true);

        public IEnumerable<ISetting> GetSettings()
        {
            yield return LoadLastDatabaseOnStartup;
        }
    }

    public class SpreadsheetSettings : ISettingGroup
    {
        public Setting<bool> OpenSpreadsheetAfterExport { get; } = new(nameof(OpenSpreadsheetAfterExport), true);

        public Setting<string> ExportDateTimeFormat { get; } = new(nameof(ExportDateTimeFormat), "yyyy-MM-dd HH:mm:ss");
        public Setting<char> ExportCsvDelimiter { get; } = new(nameof(ExportCsvDelimiter), ';');

        public ICommand<string> SetExportCsvDelimiterCommand { get; }

        public IEnumerable<ISetting> GetSettings()
        {
            yield return OpenSpreadsheetAfterExport;
            yield return ExportDateTimeFormat;
            yield return ExportCsvDelimiter;
        }
    }

    public sealed class Workspace : NotifyPropertyChanged, ICommandContext, ISettingGroup
    {
        public Workspace(BackgroundJobScheduler scheduler, IWindowService windowService, IDialogService dialogService, UiOptionManager optionManager, ShortcutService shortcutService, CommandBindingCollection routedCommandBindings, InputBindingCollection inputBindings, DpiScale dpi, Action bringToFront)
        {
            this.DialogService = dialogService;
            this.Scheduler = scheduler;
            this.ShortcutService = shortcutService;
            this.OptionManager = optionManager;
            this.WindowService = windowService;
            this.routedCommandBindings = routedCommandBindings;
            this.inputBindings = inputBindings;
            this.Dpi = dpi;
            this.bringToFront = bringToFront;
            this.OpenFileCommand = Commands.Execute<IFile>(FileUtils.Open).If(f => f?.IsLoaded == true);
            this.OpenShortcutCommand = Commands.Execute<IShortcut>(s => shortcutService.Open(s)).IfNotNull();

            WindowService.AddTool(LogTool = new LogTool(shortcutService));
            WindowService.AddTool(JobsTool = new JobsTool(scheduler));
            WindowService.AddTool(PropertyEditorTool = new PropertyEditorTool(windowService));

            BindCommand(RoutedCommands.OpenFile, OpenFileCommand);
            BindCommand(RoutedCommands.OpenShortcut, OpenShortcutCommand);

            SetupOptions();
            AddGesture(Commands.Execute(DebugInformation), new KeyGesture(Key.D, ModifierKeys.Control | ModifierKeys.Shift));
            AddGesture(Commands.Execute(DebugFocus), new KeyGesture(Key.I, ModifierKeys.Control | ModifierKeys.Shift));

            shortcutService.RegisterHandler(new SettingsShortcutHandler(ShowSettings));
        }

        public string? ProgressText
        {
            get => progressText;
            set => SetField(ref progressText, value);
        }

        private string? progressText;

        public double Progress
        {
            get => progress;
            set => SetField(ref progress, value);
        }

        private double progress;

        public DpiScale Dpi { get; }

        public WorkspaceToolbar Toolbar { get; } = new WorkspaceToolbar();
        public WorkspaceMenu MainMenu { get; } = new WorkspaceMenu();

        public LogTool LogTool { get; }
        public JobsTool JobsTool { get; }
        public PropertyEditorTool PropertyEditorTool { get; }

        public WorkspaceSettings WorkspaceSettings { get; } = new WorkspaceSettings();
        public SpreadsheetSettings SpreadsheetSettings { get; } = new SpreadsheetSettings();

        private const double DefaultSidePanelWidth = 280;
        private const double DefaultBottomPanelHeight = 220;

        public Setting<double> LeftPanelWidth { get; } = new(nameof(LeftPanelWidth), DefaultSidePanelWidth);
        public Setting<double> RightPanelWidth { get; } = new(nameof(RightPanelWidth), DefaultSidePanelWidth);
        public Setting<double> BottomPanelHeight { get; } = new(nameof(BottomPanelHeight), DefaultBottomPanelHeight);
        public Setting<Dictionary<string, bool>> ToolVisibility { get; } = new(nameof(ToolVisibility), new Dictionary<string, bool>());

        public Setting<bool> ViewStatusBar { get; } = new Setting<bool>(nameof(ViewStatusBar), true);

        public Setting<bool> ViewToolbar { get; } = new Setting<bool>(nameof(ViewToolbar), true);

        IEnumerable<ISetting> ISettingGroup.GetSettings()
        {
            yield return LeftPanelWidth;
            yield return RightPanelWidth;
            yield return BottomPanelHeight;
            yield return ToolVisibility;
            yield return ViewStatusBar;
            yield return ViewToolbar;
            foreach (var setting in WorkspaceSettings.GetSettings())
                yield return setting;
            foreach (var setting in SpreadsheetSettings.GetSettings())
                yield return setting;
        }

        private IReadOnlyList<Tool> defaultVisibleTools = Array.Empty<Tool>();
        private readonly CommandBindingCollection routedCommandBindings;
        private readonly InputBindingCollection inputBindings;
        private readonly Action bringToFront;

        public BackgroundJobScheduler Scheduler { get; }
        public IWindowService WindowService { get; }
        public IDialogService DialogService { get; }
        public UiOptionManager OptionManager { get; }

        public ShortcutService ShortcutService { get; }

        public bool IsQuitting { get; private set; }

        public AdviceManager AdviceManager { get; } = new AdviceManager();

        public ObservableCollection<object> Bars { get; } = new ObservableCollection<object>();

        public ICommand<IFile> OpenFileCommand { get; }
        public ICommand<IShortcut> OpenShortcutCommand { get; }

        public void BindCommand(RoutedCommand routedCommand, ICommand command)
        {
            routedCommandBindings.Add(routedCommand, command);
        }

        public void BindCommand(RoutedCommand routedCommand, Action action, Func<bool>? canExecute = null)
        {
            if (canExecute != null)
                routedCommandBindings.Add(routedCommand, action, canExecute);
            else
                routedCommandBindings.Add(routedCommand, action);
        }

        public object? GetCommandParameter(Type type) => WindowService.ActiveContent?.GetCommandParameter(type);

        bool HasUnsavedProgress()
        {
            return WindowService.Pages.Any(doc => doc.IsDirty);
        }

        public bool MaybeConfirmLoseUnsavedProgress(string caption)
        {
            if (HasUnsavedProgress())
                return DialogService.ShowQuestion("All unsaved progress will be lost. Proceed?", caption);
            else
                return true;
        }

        public void Quit()
        {
            this.IsQuitting = true;
            Application.Current.Shutdown();
        }

        public void OpenBrowser(string title, string contentId, bool useExternalBrowser, Uri uri)
        {
            if (useExternalBrowser)
            {
                OpenExternalBrowser(uri);
                return;
            }

            var doc = WindowService.Pages.OfType<BrowserPage>().FirstOrDefault(d => d.ContentId == contentId);
            if (doc == null)
            {
                doc = new BrowserPage(title, contentId, uri);
                WindowService.AddPage(doc);
            }
            WindowService.ActiveContent = doc;
        }

        public static void OpenExternalBrowser(Uri uri)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = uri.ToString(),
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        public void ShowSettings(string? settingsGroup)
        {
            var settingsPage = WindowService.OpenSingletonPage(() => new SettingsPage(WorkspaceSettings, SpreadsheetSettings));
            if (settingsGroup != null)
                settingsPage.OpenTab(settingsGroup);
        }

        public static void DebugInformation()
        {
            GarbageMonitor.GarbageCollect();
            GarbageMonitor.Report();
            ControlPools.Report();
        }

        public static void DebugFocus()
        {
            IInputElement focusedControl = Keyboard.FocusedElement;
            Logger.LogDebug($"Focused control: {focusedControl}");
            if (focusedControl is DependencyObject depObj)
                foreach (var obj in VisualTreeHelperEx.GetParentTree(depObj))
                {
                    Logger.LogDebug($"Focused parent: {obj}");
                }
        }

        private void SetupOptions()
        {
            var undoOption = new UiCommandOption("Undo", Fugue.Icons.ArrowReturn270Left, ApplicationCommands.Undo);
            OptionManager.AddMenuOption(undoOption, "Edit#0", showInToolbar: true);

            var redoOption = new UiCommandOption("Redo", Fugue.Icons.ArrowReturn270, ApplicationCommands.Redo);
            OptionManager.AddMenuOption(redoOption, "Edit#0", showInToolbar: true);

            var cutOption = new UiCommandOption("Cut", Fugue.Icons.Scissors, ApplicationCommands.Cut);
            OptionManager.AddMenuOption(cutOption, "Edit#10", showInToolbar: true);

            var copyOption = new UiCommandOption("Copy", Fugue.Icons.BlueDocumentCopy, ApplicationCommands.Copy);
            OptionManager.AddMenuOption(copyOption, "Edit#10", showInToolbar: true);

            var pasteOption = new UiCommandOption("Paste", Fugue.Icons.ClipboardPaste, ApplicationCommands.Paste);
            OptionManager.AddMenuOption(pasteOption, "Edit#10", showInToolbar: true);

            var deleteOption = new UiCommandOption("Delete", Fugue.Icons.CrossScript, ApplicationCommands.Delete);
            OptionManager.AddMenuOption(deleteOption, "Edit#10", showInToolbar: true);

            var findOption = new UiCommandOption("Find", Fugue.Icons.Magnifier, RoutedCommands.Find);
            OptionManager.AddMenuOption(findOption, "Edit#20", showInToolbar: true);

            var findNextOption = new UiCommandOption("Find Next", null, RoutedCommands.FindNext);
            OptionManager.AddMenuOption(findNextOption, "Edit#20");

            var findPreviousOption = new UiCommandOption("Find Previous", null, RoutedCommands.FindPrevious);
            OptionManager.AddMenuOption(findPreviousOption, "Edit#20");

            var expandSelectionOption = new UiCommandOption("Expand Selection", null, RoutedCommands.ExpandSelection);
            OptionManager.AddMenuOption(expandSelectionOption, "Edit#30");

            var collapseSelectionOption = new UiCommandOption("Collapse Selection", null, RoutedCommands.CollapseSelection);
            OptionManager.AddMenuOption(collapseSelectionOption, "Edit#30");

            var expandAllOption = new UiCommandOption("Expand All", null, RoutedCommands.ExpandAll);
            OptionManager.AddMenuOption(expandAllOption, "Edit#30");

            var collapseAllOption = new UiCommandOption("Collapse All", null, RoutedCommands.CollapseAll);
            OptionManager.AddMenuOption(collapseAllOption, "Edit#30");

            var viewStatusBarOption = new UiToggleOption("Status Bar", null, ViewStatusBar);
            OptionManager.AddMenuOption(viewStatusBarOption, "View#0");

            var viewToolbarOption = new UiToggleOption("Toolbar", null, ViewToolbar);
            OptionManager.AddMenuOption(viewToolbarOption, "View#0");

            var logOption = new UiCommandOption("Log", Fugue.Icons.Notebook, LogTool.Show);
            OptionManager.AddMenuOption(logOption, "View#10");

            var tasksOption = new UiCommandOption("Tasks", Fugue.Icons.ApplicationTask, JobsTool.Show);
            OptionManager.AddMenuOption(tasksOption, "View#10");

            var propertyEditorOption = new UiCommandOption("Property Editor", Fugue.Icons.Wrench, PropertyEditorTool.Show);
            OptionManager.AddMenuOption(propertyEditorOption, "View#10");

            var closeAllDocumentsOption = new UiCommandOption("Close All Documents", null, WindowService.CloseAllPagesCommand);
            OptionManager.AddMenuOption(closeAllDocumentsOption, "Window#0");

            var moveToOtherGroupCommand = Commands.Execute(() => WindowService.MoveToOtherGroupCommand.Execute(WindowService.ActiveContent as Page))
                .If(() => WindowService.MoveToOtherGroupCommand.CanExecute(WindowService.ActiveContent as Page));
            var moveToOtherGroupOption = new UiCommandOption("Move to Other Group", null, moveToOtherGroupCommand);
            OptionManager.AddMenuOption(moveToOtherGroupOption, "Window#0");
            AddGesture(moveToOtherGroupCommand, new KeyGesture(Key.Oem5, ModifierKeys.Control));

            var resetLayoutOption = new UiCommandOption("Reset Layout", null, ResetLayout);
            OptionManager.AddMenuOption(resetLayoutOption, "Window#0");

            var settingsOption = new UiCommandOption("Settings", Fugue.Icons.Gear, () => ShowSettings(null));
            OptionManager.AddMenuOption(settingsOption, "Tools#10", showInToolbar: true);

            var aboutCommand = Commands.Execute(() => DialogService.ShowDialog(new AboutDialog()));
            var aboutOption = new UiCommandOption("About Hercules...", Fugue.Icons.InformationWhite, aboutCommand);
            OptionManager.AddMenuOption(aboutOption, "Help#20");
        }

        // Called once all modules have added their tools and the settings are loaded
        public void RestoreLayout(bool reset)
        {
            defaultVisibleTools = WindowService.Tools.Where(t => t.IsVisible).ToList();
            foreach (var tool in WindowService.Tools)
            {
                tool.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(Tool.IsVisible))
                        ToolVisibility.Value = WindowService.Tools.ToDictionary(t => t.ContentId!, t => t.IsVisible);
                };
            }

            if (reset)
            {
                ResetLayout();
                return;
            }
            foreach (var tool in WindowService.Tools)
            {
                if (ToolVisibility.Value.TryGetValue(tool.ContentId!, out var isVisible))
                    tool.IsVisible = isVisible;
            }
        }

        public void ResetLayout()
        {
            LeftPanelWidth.Value = DefaultSidePanelWidth;
            RightPanelWidth.Value = DefaultSidePanelWidth;
            BottomPanelHeight.Value = DefaultBottomPanelHeight;
            foreach (var tool in WindowService.Tools)
                tool.IsVisible = defaultVisibleTools.Contains(tool);
        }

        public void BuildUI()
        {
            MainMenu.Build(OptionManager.Options.OrderBy(c => c.CategoryPath, OptionManager), AdviceManager);
            Toolbar.Build(OptionManager.Options.Where(c => c.ShowInToolbar).OrderBy(c => c.CategoryPath, OptionManager));
        }

        public void AddGesture(ICommand command, InputGesture gesture)
        {
            inputBindings.Add(new InputBinding(command, gesture));
        }

        public void AddGesture(UiCommandOption uiOption)
        {
            if (uiOption.Gesture != null)
                AddGesture(uiOption.Command, uiOption.Gesture);
        }

        public void BringToFront()
        {
            bringToFront();
        }
    }
}
