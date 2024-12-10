using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Xml;

namespace Hercules.Import.View
{
    /// <summary>
    /// Interaction logic for ImportTable.xaml
    /// </summary>
    [ViewModelType(typeof(ImportTablePage))]
    public partial class ImportTablePageView : UserControl
    {
        //// public SyntaxValidator SyntaxValidator { get; private set; }

        public ImportTablePageView()
        {
            InitializeComponent();

            //// textEditor.TextArea.TextEntering += textEditor_TextArea_TextEntering;
            //// textEditor.TextArea.TextEntered += textEditor_TextArea_TextEntered;
            //// SyntaxValidator = new SyntaxValidator();
            //// textEditor.TextArea.TextView.LineTransformers.Add(new SyntaxErrorColorizer(SyntaxValidator, decoration));

            using Stream s = File.OpenRead("SyntaxHighlight\\JavaScript.xshd");
            using XmlTextReader reader = new XmlTextReader(s);
            textEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
        }

        public ImportTablePage ViewModel => (ImportTablePage)DataContext;

        public void SetupRawTable()
        {
            var grid = DataGrid;
            var table = ViewModel.RawTable;
            grid.Columns.Clear();
            if (table == null)
                grid.ItemsSource = null;
            else
            {
                grid.ItemsSource = table.Rows;
                for (int i = 0; i < table.ColumnCount; i++)
                {
                    var binding = new Binding($"Items[{i}].Value");
                    grid.Columns.Add(new DataGridTextColumn() { Header = i.ToString(CultureInfo.InvariantCulture), Binding = binding });
                }
            }
        }

        public void SetupMapTable()
        {
            var grid = DataGrid;
            var table = ViewModel.MapTable;
            grid.Columns.Clear();
            if (table == null)
                grid.ItemsSource = null;
            else
            {
                grid.ItemsSource = table.Rows;
                int i = 0;
                foreach (var col in table.Columns)
                {
                    var binding = new Binding($"Items[{i}].Text");
                    grid.Columns.Add(new DataGridTextColumn { Header = col.Title, Binding = binding });
                    i++;
                }
            }
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ViewModel.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == "MapTable")
                    SetupMapTable();
            };
        }
    }
}
