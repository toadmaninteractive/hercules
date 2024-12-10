using Hercules.Shell;
using ICSharpCode.AvalonEdit.Document;

namespace Hercules.Scripting
{
    public class TextPage : Page
    {
        public TextDocument TextDocument { get; }
        public string? Syntax { get; }
        public string? SyntaxFile => Syntax == null ? null : $"SyntaxHighlight\\{Syntax}.xshd";

        public TextPage(string title, string text, string? syntax)
        {
            Title = title;
            TextDocument = new TextDocument(text);
            Syntax = syntax;
        }
    }
}
