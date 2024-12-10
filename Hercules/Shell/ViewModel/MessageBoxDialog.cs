using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Hercules.Shell
{
    [Flags]
    public enum DialogButtons
    {
        Ok = 1,
        Cancel = 2,
        Yes = 4,
        YesToAll = 8,
        No = 16,
        NoToAll = 32,
        Close = 64,
        Reset = 128,
    }

    public enum DialogIcon
    {
        Exclamation,
        Question,
        Information,
        Error,
        DatabaseError,
        Smile,
    }

    public class MessageBoxDialog : Dialog
    {
        public MessageBoxDialog(string message, string title, DialogButtons buttons, DialogButtons @default, DialogIcon icon)
        {
            Title = title;
            Message = message;
            Icon = icon;
            Default = @default;
            Result = DialogButtons.Close;
            Buttons = new ObservableCollection<DialogButtons>(Enum.GetValues(typeof(DialogButtons)).Cast<DialogButtons>().Where(b => buttons.HasFlag(b)));
            ResultCommand = Commands.Execute<DialogButtons>(b => { Result = b; SetDialogResult(true); });
        }

        public ObservableCollection<DialogButtons> Buttons { get; }

        public string Message { get; }

        public DialogIcon Icon { get; }

        public ICommand ResultCommand { get; }

        public DialogButtons Result { get; set; }

        public DialogButtons Default { get; set; }
    }
}
