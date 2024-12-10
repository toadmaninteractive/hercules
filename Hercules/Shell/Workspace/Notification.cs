using System;
using System.Windows.Input;

namespace Hercules.Shell
{
    public abstract class Notification : NotifyPropertyChanged
    {
        public ICommand CloseCommand { get; }

        public NotificationService? Owner { get; set; }

        public void Close()
        {
            Owner?.Remove(this);
        }

        protected Action DoAndClose(Action action)
        {
            return () => { Close(); action(); };
        }

        public object? Source { get; }

        protected Notification(object? source)
        {
            Source = source;
            CloseCommand = Commands.Execute(Close);
        }
    }
}
