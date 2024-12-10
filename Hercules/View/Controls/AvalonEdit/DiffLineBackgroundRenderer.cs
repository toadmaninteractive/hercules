using ICSharpCode.AvalonEdit.Rendering;
using JsonDiff;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Hercules.Controls.AvalonEdit
{
    public class DiffLineBackgroundRenderer : IBackgroundRenderer
    {
        private static readonly Pen pen;

        private static readonly SolidColorBrush removedBackground;
        private static readonly SolidColorBrush addedBackground;
        private static readonly SolidColorBrush skipBackground;

        public IDiffLines? Lines { get; set; }

        static DiffLineBackgroundRenderer()
        {
            removedBackground = new SolidColorBrush(Color.FromRgb(0xff, 0xdd, 0xdd));
            removedBackground.Freeze();
            addedBackground = new SolidColorBrush(Color.FromRgb(0x7f, 0xff, 0x00));
            addedBackground.Freeze();
            skipBackground = new SolidColorBrush(Color.FromRgb(0xdc, 0xdc, 0xdc));
            skipBackground.Freeze();

            var blackBrush = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            blackBrush.Freeze();
            pen = new Pen(blackBrush, 0.0);
        }

        public DiffLineBackgroundRenderer(IDiffLines? lines)
        {
            this.Lines = lines;
        }

        public KnownLayer Layer => KnownLayer.Background;

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (Lines == null)
                return;

            foreach (var v in textView.VisualLines)
            {
                var rc = BackgroundGeometryBuilder.GetRectsFromVisualSegment(textView, v, 0, 1000).First();
                // NB: This lookup to fetch the doc line number isn't great, we could
                // probably do it once then just increment.
                var linenum = v.FirstDocumentLine.LineNumber - 1;
                var mode = Lines.GetLineStyle(linenum);
                if (mode == DiffLineStyle.Normal)
                    continue;

                var brush = default(Brush);
                switch (mode)
                {
                    case DiffLineStyle.Added:
                        brush = addedBackground;
                        break;
                    case DiffLineStyle.Deleted:
                        brush = removedBackground;
                        break;
                    case DiffLineStyle.Skip:
                        brush = skipBackground;
                        break;
                }

                drawingContext.DrawRectangle(brush, pen,
                    new Rect(0, rc.Top, textView.ActualWidth, rc.Height));
            }
        }
    }
}
