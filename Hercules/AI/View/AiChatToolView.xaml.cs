﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
