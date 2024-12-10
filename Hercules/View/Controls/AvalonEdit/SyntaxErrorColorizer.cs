using Hercules.Scripting;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System.Windows;

namespace Hercules.Controls.AvalonEdit
{
    public class SyntaxErrorColorizer : DocumentColorizingTransformer
    {
        readonly SyntaxValidator syntaxValidator;
        readonly TextDecorationCollection decoration;

        public SyntaxErrorColorizer(SyntaxValidator syntaxValidator, TextDecorationCollection decoration)
        {
            this.syntaxValidator = syntaxValidator;
            this.decoration = decoration;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            foreach (var err in syntaxValidator.Errors)
            {
                if (err.Position >= line.Offset && err.Position < line.EndOffset)
                {
                    ChangeLinePart(err.Position, line.EndOffset,
                                   visualLineElement => visualLineElement.TextRunProperties.SetTextDecorations(decoration));
                }
            }
        }
    }
}
