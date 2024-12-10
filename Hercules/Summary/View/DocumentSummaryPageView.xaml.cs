using Hercules.Converters;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;
using Telerik.Windows.Data;

namespace Hercules.Summary.View
{
    [ViewModelType(typeof(DocumentSummaryPage))]
    public partial class DocumentSummaryPageView : UserControl
    {
        public DocumentSummaryPageView()
        {
            InitializeComponent();
        }

        public DocumentSummaryPage Model => (DocumentSummaryPage)DataContext;

        public void ResetTable()
        {
            DataGrid.Columns.Clear();

            var modifiedBrush = Brushes.LightGreen;
            var nullTemplate = (DataTemplate)Resources["NullTableCell"];

            foreach (var column in Model.Table.Columns)
            {
                var binding = new Binding(column.PropertyName);
                DataTemplate? editorTemplate = null;
                Style? editorStyle = null;

                Style cellStyle = new Style(typeof(GridViewCellBase));
                var modifiedTrigger = new DataTrigger { Binding = new Binding(column.CellPropertyName + ".IsModified"), Value = true };
                modifiedTrigger.Setters.Add(new Setter { Property = GridViewCellBase.BackgroundProperty, Value = modifiedBrush });
                cellStyle.Triggers.Add(modifiedTrigger);
                cellStyle.Setters.Add(new Setter { Property = GridViewCellBase.PaddingProperty, Value = new Thickness(0) });
                cellStyle.Setters.Add(new Setter { Property = GridViewCellBase.MarginProperty, Value = new Thickness(0) });
                cellStyle.Seal();

                DataTemplate cellTemplate = new DataTemplate(typeof(TableCell));
                var cellInnerTemplate = Resources[column.CellType.Name];
                var cellContentControl = new FrameworkElementFactory(typeof(ContentControl));
                cellTemplate.VisualTree = cellContentControl;
                cellContentControl.SetBinding(ContentControl.ContentTemplateProperty, new Binding(column.CellPropertyName + ".Value") { Converter = new EqualityConverter(nullTemplate, cellInnerTemplate), ConverterParameter = null });
                cellContentControl.SetBinding(ContentControl.ContentProperty, new Binding(column.CellPropertyName));
                cellContentControl.SetValue(ContentControl.FocusableProperty, false);
                cellTemplate.Seal();

                if (!column.IsReadOnly)
                {
                    editorTemplate = (DataTemplate)Resources[new DataTemplateKey(column.CellType)];
                    editorTemplate.Seal();
                    editorStyle = new Style(typeof(FrameworkElement));
                    editorStyle.Setters.Add(new Setter { Property = FrameworkElement.DataContextProperty, Value = new Binding(column.CellPropertyName) });
                    editorStyle.Seal();
                }

                var dataColumn = new GridViewDataColumn
                {
                    Tag = column,
                    CellStyle = cellStyle,
                    CellTemplate = cellTemplate,
                    EditorStyle = editorStyle,
                    CellEditTemplate = editorTemplate,
                    Header = column.Name,
                    DataMemberBinding = binding,
                    DataType = column.Type.Type,
                    IsReadOnly = column.IsReadOnly,
                    // EditTriggers = GridViewEditTriggers.CellClick | GridViewEditTriggers.CurrentCellClick | GridViewEditTriggers.F2 | GridViewEditTriggers.TextInput
                };

                if (column.IsId)
                    dataColumn.AggregateFunctions.Add(new CountFunction { Caption = "Count: " });

                DataGrid.Columns.Add(dataColumn);
            }
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ResetTable();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataGrid.CurrentCell is { IsMouseOver: true } && (DataGrid.CurrentCell.Column.IsReadOnly || DataGrid.IsReadOnly))
            {
                var value = DataGrid.CurrentCell.Value;
                if (value != null)
                {
                    var text = value.ToString()!;
                    if (Model.Project!.Database.Documents.ContainsKey(text))
                        Model.DocumentsModule.EditDocument(text);
                }
            }
        }
    }
}
