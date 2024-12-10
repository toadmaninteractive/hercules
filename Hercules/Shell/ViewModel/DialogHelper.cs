namespace Hercules.Shell
{
    public static class DialogHelper
    {
        public static DialogButtons ShowMessageBox(this IDialogService dialogService, string message, string caption, DialogButtons buttons, DialogButtons @default, DialogIcon icon)
        {
            var box = new MessageBoxDialog(message, caption, buttons, @default, icon);
            dialogService.ShowDialog(box);
            return box.Result;
        }

        public static void ShowError(this IDialogService dialogService, string message, string caption = "Error")
        {
            ShowMessageBox(dialogService, message, caption, DialogButtons.Ok, DialogButtons.Ok, DialogIcon.Error);
        }

        public static void ShowWarning(this IDialogService dialogService, string message, string caption = "Warning")
        {
            ShowMessageBox(dialogService, message, caption, DialogButtons.Ok, DialogButtons.Ok, DialogIcon.Exclamation);
        }

        public static bool ShowQuestion(this IDialogService dialogService, string message, string caption = "Confirmation")
        {
            return ShowMessageBox(dialogService, message, caption, DialogButtons.Yes | DialogButtons.No, DialogButtons.Yes, DialogIcon.Question) == DialogButtons.Yes;
        }

        public static (DialogButtons button, string value) ShowPromptDialog(this IDialogService dialogService, string message, string caption, string? defaultValue, DialogButtons buttons, DialogButtons defaultButton, DialogIcon icon)
        {
            var box = new PromptDialog(message, caption, defaultValue, buttons, defaultButton, icon);
            dialogService.ShowDialog(box);
            return (box.Result, box.Value);
        }

        public static string? ShowPromptDialog(this IDialogService dialogService, string message, string caption, string? defaultValue = null)
        {
            (var button, var result) = ShowPromptDialog(dialogService, message, caption, defaultValue, DialogButtons.Ok | DialogButtons.Cancel, DialogButtons.Ok, DialogIcon.Question);
            return button == DialogButtons.Ok ? result : null;
        }
    }
}
