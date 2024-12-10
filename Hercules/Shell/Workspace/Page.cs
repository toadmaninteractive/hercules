using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace Hercules.Shell
{
    public interface IClosePageService
    {
        ICommand<Page> CloseAllButThisCommand { get; }
        ICommand CloseAllPagesCommand { get; }
        ClosePageContext GetClosePageContext(CloseDirtyPageAction closeDirtyPageAction, bool isBulk = false);
    }

    public abstract class Page : DockingLayoutItem
    {
        protected IClosePageService? ClosePageService { get; private set; }

        protected Page()
        {
            GarbageMonitor.Register(this);
            CloseCommand = Commands.Execute(() => Close(CloseDirtyPageAction.AskConfirmation));
            RoutedCommandBindings.Add(ApplicationCommands.Close, CloseCommand);
            Services = new List<object>();
        }

        bool isDirty;

        public bool IsDirty
        {
            get => isDirty;
            set => SetField(ref isDirty, value);
        }

        public void AssignClosePageService(IClosePageService closePageService)
        {
            ClosePageService = closePageService;
            CloseAllButThisCommand = closePageService.CloseAllButThisCommand.For(this);
        }

        public ICommand CloseCommand { get; }
        public ICommand? CloseAllButThisCommand { get; private set; }
        public ICommand? CloseAllPagesCommand => ClosePageService?.CloseAllPagesCommand;

        public IList Services { get; }

        public NotificationService Notifications { get; } = new NotificationService();

        public void Broadcast<T>(Action<T> action)
        {
            foreach (var service in Services.OfType<T>())
                action(service);
        }

        public void Close(CloseDirtyPageAction closeDirtyPageAction)
        {
            var context = ClosePageService!.GetClosePageContext(closeDirtyPageAction);
            Close(context);
        }

        public void Close(ClosePageContext context)
        {
            if (CanClose(context))
            {
                context.Closed(this);
                DoClose();
            }
        }

        private void DoClose()
        {
            Broadcast<IDisposable>(disp => disp.Dispose());
            OnClose();
        }

        private bool CanClose(ClosePageContext context)
        {
            if (!IsDirty)
                return true;
            return context.DirtyAction switch
            {
                CloseDirtyPageAction.Keep => false,
                CloseDirtyPageAction.ForceClose => true,
                _ => AskCloseConfirmation(context)
            };
        }

        protected virtual bool AskCloseConfirmation(ClosePageContext context)
        {
            return true;
        }

        protected virtual void OnClose()
        {
        }
    }
}
