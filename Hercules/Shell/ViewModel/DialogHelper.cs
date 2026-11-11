namespace Hercules.Shell
{
    public static class DialogHelper
    {
        extension(IDialogService dialogService)
        {
            public DialogButtons ShowMessageBox(string message, string caption, DialogButtons buttons, DialogButtons @default, DialogIcon icon)
            {
                var box = new MessageBoxDialog(message, caption, buttons, @default, icon);
                dialogService.ShowDialog(box);
                return box.Result;
            }

            public void ShowError(string message, string caption = "Error")
            {
                ShowMessageBox(dialogService, message, caption, DialogButtons.Ok, DialogButtons.Ok, DialogIcon.Error);
            }

            public void ShowWarning(string message, string caption = "Warning")
            {
                ShowMessageBox(dialogService, message, caption, DialogButtons.Ok, DialogButtons.Ok, DialogIcon.Exclamation);
            }

            public bool ShowQuestion(string message, string caption = "Confirmation")
            {
                return ShowMessageBox(dialogService, message, caption, DialogButtons.Yes | DialogButtons.No, DialogButtons.Yes, DialogIcon.Question) == DialogButtons.Yes;
            }

            public (DialogButtons button, string value) ShowPromptDialog(string message, string caption, string? defaultValue, DialogButtons buttons, DialogButtons defaultButton, DialogIcon icon)
            {
                var box = new PromptDialog(message, caption, defaultValue, buttons, defaultButton, icon);
                dialogService.ShowDialog(box);
                return (box.Result, box.Value);
            }

            public string? ShowPromptDialog(string message, string caption, string? defaultValue = null)
            {
                (var button, var result) = ShowPromptDialog(dialogService, message, caption, defaultValue, DialogButtons.Ok | DialogButtons.Cancel, DialogButtons.Ok, DialogIcon.Question);
                return button == DialogButtons.Ok ? result : null;
            }
        }
    }
}
