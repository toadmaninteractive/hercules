using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Microsoft.Xaml.Behaviors;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Xml;

namespace Hercules.Controls
{
    public class SyntaxHighlightBehavior : Behavior<TextEditor>
    {
        public static readonly DependencyProperty SyntaxProperty = DependencyProperty.Register("Syntax", typeof(string), typeof(SyntaxHighlightBehavior), new PropertyMetadata(null, OnSyntaxPropertyChanged));

        public string? Syntax
        {
            get => (string)GetValue(SyntaxProperty);
            set => SetValue(SyntaxProperty, value);
        }

        private static void OnSyntaxPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SyntaxHighlightBehavior)d).SetSyntax(e.NewValue as string);
        }

        protected override void OnAttached()
        {
            SetSyntax(Syntax);
        }

        void SetSyntax(string? syntax)
        {
            try
            {
                if (syntax != null && AssociatedObject != null)
                {
                    var baseFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    var syntaxFile = Path.Combine(baseFolder!, syntax);
                    using Stream s = File.OpenRead(syntaxFile);
                    using XmlTextReader reader = new XmlTextReader(s);
                    AssociatedObject.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                }
            }
            catch (Exception exception)
            {
                Logger.LogException($"Failed to load syntax {syntax}", exception);
            }
        }
    }
}
