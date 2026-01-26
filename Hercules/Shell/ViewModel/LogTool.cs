using Hercules.Shortcuts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace Hercules.Shell
{
    public class LogTool : Tool
    {
        public LogTool(ShortcutService shortcutService)
        {
            this.Title = "Log";
            this.ContentId = "{LogViewer}";
            this.IsVisible = true;
            this.Pane = "BottomToolsPane";
            this.ShortcutService = shortcutService;
            var currentScheduler = DispatcherScheduler.Current;
            Task.Run(() => subscription = Logger.Events.BufferWhenAvailable(TimeSpan.FromSeconds(0.1)).ObserveOn(currentScheduler).Subscribe(AddRange));
            eventsView = CollectionViewSource.GetDefaultView(Events);
            eventsView.Filter += OnFilter;
            ClearCommand = Commands.Execute(Events.Clear);
            CopyAllCommand = Commands.Execute(CopyAll);
            CopyCommand = Commands.Execute<IList>(Copy);
            GoToCommand = Commands.Execute<IList>(GoTo);
            ClearFilterCommand = Commands.Execute(ClearFilter);
        }

        private bool OnFilter(object obj)
        {
            var e = (LogEvent)obj;
            if (!(e.Level switch
            {
                LogLevel.Error => ShowErrors,
                LogLevel.Warning => ShowWarnings,
                LogLevel.Debug => ShowDebug,
                _ => ShowInfo
            }))
                return false;
            if (!string.IsNullOrEmpty(filter) && !e.Message.Contains(filter))
                return false;
            return true;
        }

        public ShortcutService ShortcutService { get; }

        public ObservableCollection<LogEvent> Events { get; } = new();
        private readonly ICollectionView eventsView;

        public ICommand ClearCommand { get; }
        public ICommand CopyCommand { get; }
        public ICommand CopyAllCommand { get; }
        public ICommand GoToCommand { get; }

        public event Action OnChanged = delegate { };

        private IDisposable? subscription;

        private string filter = "";

        public string Filter
        {
            get => filter;
            set
            {
                if (SetField(ref filter, value))
                {
                    eventsView.Refresh();
                }
            }
        }

        public ICommand ClearFilterCommand { get; }

        private bool showErrors = true;
        private bool showWarnings = true;
        private bool showInfo = true;
        private bool showDebug = true;

        public bool ShowErrors
        {
            get => showErrors;
            set
            {
                if (SetField(ref showErrors, value))
                    eventsView.Refresh();
            }
        }

        public bool ShowWarnings
        {
            get => showWarnings;
            set
            {
                if (SetField(ref showWarnings, value))
                    eventsView.Refresh();
            }
        }

        public bool ShowInfo
        {
            get => showInfo;
            set
            {
                if (SetField(ref showInfo, value))
                    eventsView.Refresh();
            }
        }

        public bool ShowDebug
        {
            get => showDebug;
            set
            {
                if (SetField(ref showDebug, value))
                    eventsView.Refresh();
            }
        }

        void AddRange(IList<LogEvent> events)
        {
            if (events.Count > 0)
            {
                Events.AddRange(events);
                OnChanged();
            }
        }

        void CopyAll()
        {
            Clipboard.SetText(string.Join(Environment.NewLine, Events.Select(s => s.Copy())));
        }

        void Copy(object selected)
        {
            Clipboard.SetText(string.Join(Environment.NewLine, ((IEnumerable)selected).Cast<LogEvent>().Select(s => s.Copy())));
        }

        void GoTo(IList selected)
        {
            var shortcuts = selected.Cast<LogEvent>().Where(e => e.Shortcut != null).Select(e => e.Shortcut!);
            foreach (var shortcut in shortcuts)
                ShortcutService.Open(shortcut);
        }

        void ClearFilter()
        {
            Filter = "";
        }
    }
}
