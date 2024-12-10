using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Hercules.Shell
{
    public class PromptDialog : Dialog
    {
        public PromptDialog(string message, string title, string? defaultValue, DialogButtons buttons, DialogButtons @default, DialogIcon icon)
        {
            Title = title;
            Message = message;
            valueField = defaultValue ?? "";
            Icon = icon;
            Default = @default;
            Result = DialogButtons.Close;
            Buttons = new ObservableCollection<DialogButtons>(Enum.GetValues(typeof(DialogButtons)).Cast<DialogButtons>().Where(b => buttons.HasFlag(b)));
            ResultCommand = Commands.Execute<DialogButtons>(b => { Result = b; SetDialogResult(true); });
        }

        public ObservableCollection<DialogButtons> Buttons { get; }

        public string Message { get; }

        string valueField;
        public string Value
        {
            get => valueField;
            set => SetField(ref valueField, value);
        }

        public DialogIcon Icon { get; }

        public ICommand ResultCommand { get; }

        public DialogButtons Result { get; set; }

        public DialogButtons Default { get; set; }
    }
}
