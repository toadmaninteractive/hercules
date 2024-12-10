using System;
using System.Windows.Input;

namespace Hercules.Shell
{
    public abstract class Dialog : NotifyPropertyChanged
    {
        protected Dialog()
        {
            OkCommand = Commands.Execute(() => SetDialogResult(true)).If(() => !IsClosed && IsOkEnabled());
        }

        string? title;

        public string? Title
        {
            get => title;
            set => SetField(ref title, value);
        }

        public bool IsClosed { get; private set; }

        public ICommand OkCommand { get; }

        public void SetDialogResult(bool result)
        {
            SetDialogResultImpl?.Invoke(result);
        }

        public Action<bool>? SetDialogResultImpl { get; set; }

        protected virtual bool IsOkEnabled()
        {
            return true;
        }

        public void Closed(bool result)
        {
            IsClosed = true;
            OnClose(result);
        }

        protected virtual void OnClose(bool result)
        {
        }
    }
}
