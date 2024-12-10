using System;
using System.Windows;

namespace Hercules.Summary.View
{
    /// <summary>
    /// Interaction logic for ImportColumnsDialog.xaml
    /// </summary>
    [ViewModelType(typeof(ImportColumnsDialog))]
    public partial class ImportColumnsDialogView : Window
    {
        public ImportColumnsDialogView()
        {
            InitializeComponent();
            TimeZonePicker.ItemsSource = TimeZoneInfo.GetSystemTimeZones();
        }
    }
}
