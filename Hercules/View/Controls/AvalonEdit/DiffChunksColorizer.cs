using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using JsonDiff;
using System.Windows;
using System.Windows.Media;

namespace Hercules.Controls.AvalonEdit
{
    public class DiffChunksColorizer : DocumentColorizingTransformer
    {
        private readonly Typeface selectedTypeface;
        readonly Typeface unselectedTypeface;

        public IDiffChunks? Chunks { get; set; }

        public DiffChunksColorizer(TextEditor editor)
        {
            selectedTypeface = new Typeface(editor.FontFamily, editor.FontStyle, FontWeights.ExtraBold, editor.FontStretch);
            unselectedTypeface = new Typeface(editor.FontFamily, editor.FontStyle, FontWeights.ExtraLight, editor.FontStretch);
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            var chunk = Chunks?.GetLineChunk(line.LineNumber - 1, false);
            if (chunk != null)
            {
                ChangeLinePart(line.Offset, line.EndOffset, visualLineElement => Apply(visualLineElement, chunk.Selected));
            }
        }

        void Apply(VisualLineElement visualLineElement, bool selected)
        {
            visualLineElement.TextRunProperties.SetTypeface(selected ? selectedTypeface : unselectedTypeface);
        }
    }
}
