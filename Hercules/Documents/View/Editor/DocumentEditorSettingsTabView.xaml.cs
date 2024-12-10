using Hercules.Documents.Editor;
using System;
using System.Windows.Controls;

namespace Hercules.Documents.View.Editor
{
    /// <summary>
    /// Interaction logic for DocumentEditorSettingsTabView.xaml
    /// </summary>
    [ViewModelType(typeof(DocumentEditorSettingsTab))]
    public partial class DocumentEditorSettingsTabView : UserControl
    {
        public DocumentEditorSettingsTabView()
        {
            InitializeComponent();

            TimeZonePicker.ItemsSource = TimeZoneInfo.GetSystemTimeZones();
        }
    }
}
