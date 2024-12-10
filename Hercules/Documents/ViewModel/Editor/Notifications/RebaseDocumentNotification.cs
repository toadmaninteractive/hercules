using Hercules.Shell;
using System;
using System.Windows.Input;

namespace Hercules.Documents.Editor
{
    public class RebaseDocumentNotification : Notification
    {
        public string Text { get; }
        public ICommand RebaseCommand { get; }

        public RebaseDocumentNotification(string text, Action rebaseAction, object? source)
            : base(source)
        {
            Text = text;
            RebaseCommand = Commands.Execute(() => { Close(); rebaseAction(); });
        }
    }
}
