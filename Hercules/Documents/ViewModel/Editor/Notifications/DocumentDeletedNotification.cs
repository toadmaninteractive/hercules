using Hercules.Shell;
using System;
using System.Windows.Input;

namespace Hercules.Documents.Editor
{
    public class DocumentDeletedNotification : Notification
    {
        public ICommand ConfirmAndCloseCommand { get; private set; }
        public ICommand EditAsNewCommand { get; private set; }

        public DocumentDeletedNotification(Action confirmAndClose, Action editAsNew)
            : base(null)
        {
            ConfirmAndCloseCommand = Commands.Execute(DoAndClose(confirmAndClose));
            EditAsNewCommand = Commands.Execute(DoAndClose(editAsNew));
        }
    }
}
