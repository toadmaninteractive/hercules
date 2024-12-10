using System.Windows.Controls;

namespace Hercules.Shell.View
{
    /// <summary>
    /// Interaction logic for GeneralSettingsTabView.xaml
    /// </summary>
    [ViewModelType(typeof(SpreadsheetSettingsTab))]
    public partial class SpreadsheetSettingsTabView : UserControl
    {
        public SpreadsheetSettingsTabView()
        {
            InitializeComponent();
        }

        private void Delimiter_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dataModel = (SpreadsheetSettingsTab)DataContext;
            if (sender == DelimiterComma)
                dataModel.Settings.ExportCsvDelimiter.Value = ',';
            else if (sender == DelimiterSemicolon)
                dataModel.Settings.ExportCsvDelimiter.Value = ';';
            else if (sender == DelimiterTab)
                dataModel.Settings.ExportCsvDelimiter.Value = '\t';
        }
    }
}
