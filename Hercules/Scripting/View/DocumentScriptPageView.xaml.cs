using Hercules.Controls.AvalonEdit;
using System.Windows;
using System.Windows.Controls;

namespace Hercules.Scripting.View
{
    [ViewModelType(typeof(DocumentScriptPage))]
    public partial class DocumentScriptPageView : UserControl
    {
        public SyntaxValidator SyntaxValidator { get; private set; }

        public DocumentScriptPageView()
        {
            InitializeComponent();
            SyntaxValidator = new SyntaxValidator();
            var decoration = (TextDecorationCollection)FindResource("SyntaxErrorTextDecoration");
            TextEditor.TextArea.TextView.LineTransformers.Add(new SyntaxErrorColorizer(SyntaxValidator, decoration));
        }
    }
}
