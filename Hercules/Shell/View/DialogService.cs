using System;
using System.Windows;
using System.Windows.Interop;

namespace Hercules.Shell.View
{
    public class DialogService : IDialogService
    {
        public Window ParentWindow { get; }

        public DialogService(Window parentWindow)
        {
            this.ParentWindow = parentWindow;
        }

        private bool? ShowModalForm(Window wnd)
        {
            _ = new WindowInteropHelper(wnd) { Owner = new WindowInteropHelper(ParentWindow).Handle };
            return wnd.ShowDialog();
        }

        Window CreateDialogWindow(Dialog dialog)
        {
            if (ViewModelTypes.TryGetViewTypeByViewModelType(dialog.GetType(), out var windowType))
                return (Window)Activator.CreateInstance(windowType)!;
            else
                throw new NotImplementedException("No view is registered for dialog " + dialog.GetType());
        }

        public bool ShowDialog(Dialog dialog)
        {
            var wnd = CreateDialogWindow(dialog);
            wnd.DataContext = dialog;
            dialog.SetDialogResultImpl = res => wnd.DialogResult = res;
            var result = ShowModalForm(wnd) == true;
            dialog.Closed(result);
            return result;
        }
    }
}
