using System.Windows.Controls;

namespace Hercules.AI.View
{
    /// <summary>
    /// Interaction logic for AiChatToolView.xaml
    /// </summary>
    [ViewModelType(typeof(AiChatTool))]
    public partial class AiChatToolView : UserControl
    {
        public AiChatToolView()
        {
            InitializeComponent();
        }

        public void ScrollToLast()
        {
            var scrollViewer = (ScrollViewer)ChatLog.Template.FindName("PART_ContentHost", ChatLog);
            scrollViewer.ScrollToEnd();
        }
    }
}
