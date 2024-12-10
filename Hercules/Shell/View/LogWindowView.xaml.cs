using System.Windows.Controls;

namespace Hercules.Shell.View
{
    [ViewModelType(typeof(LogTool))]
    public partial class LogWindowView : UserControl
    {
        public LogWindowView()
        {
            InitializeComponent();
        }

        public void ScrollToLast()
        {
            if (LbLog.Items.Count > 0)
                LbLog.ScrollIntoView(LbLog.Items[^1]);
        }
    }
}
