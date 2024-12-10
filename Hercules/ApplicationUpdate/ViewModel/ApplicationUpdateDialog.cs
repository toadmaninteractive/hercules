using Hercules.Shell;
using System.Windows.Input;

namespace Hercules.ApplicationUpdate
{
    public class ApplicationUpdateDialog : Dialog
    {
        public ApplicationUpdateDialog(ICommand viewReleaseNotesCommand)
        {
            Title = "Hercules Application Update";
            ViewReleaseNotesCommand = viewReleaseNotesCommand;
        }

        public ICommand ViewReleaseNotesCommand { get; }
    }
}
