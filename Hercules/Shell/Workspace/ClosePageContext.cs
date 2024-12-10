using System;

namespace Hercules.Shell
{
    public enum CloseDirtyPageAction
    {
        AskConfirmation,
        Keep,
        ForceClose,
    }

    public class ClosePageContext
    {
        public CloseDirtyPageAction DirtyAction { get; set; }
        public bool IsBatch { get; }
        private readonly Action<Page> closeAction;

        public ClosePageContext(CloseDirtyPageAction dirtyAction, bool isBatch, Action<Page> closeAction)
        {
            DirtyAction = dirtyAction;
            IsBatch = isBatch;
            this.closeAction = closeAction;
        }

        public void Closed(Page page)
        {
            closeAction(page);
        }
    }
}
