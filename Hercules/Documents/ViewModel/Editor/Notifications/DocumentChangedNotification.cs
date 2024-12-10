using Hercules.Shell;
using System;
using System.Windows.Input;

namespace Hercules.Documents.Editor
{
    public class DocumentChangedNotification : Notification
    {
        public ICommand TakeMineCommand { get; private set; }
        public ICommand TakeRemoteCommand { get; private set; }

        public DocumentChangedNotification(Action takeMine, Action takeRemote)
            : base(null)
        {
            TakeMineCommand = Commands.Execute(DoAndClose(takeMine));
            TakeRemoteCommand = Commands.Execute(DoAndClose(takeRemote));
        }
    }
}
