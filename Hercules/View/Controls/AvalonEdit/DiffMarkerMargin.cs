using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using JsonDiff;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Hercules.Controls.AvalonEdit
{
    public class DiffMarkerMargin : AbstractMargin
    {
        readonly ImageSource plus;
        readonly ImageSource minus;

        IDiffLines? diffLines;

        public IDiffLines? DiffLines
        {
            get => diffLines;
            set
            {
                if (diffLines != value)
                {
                    diffLines = value;
                    InvalidateVisual();
                }
            }
        }

        public DiffMarkerMargin()
        {
            minus = (BitmapImage)Application.Current.FindResource(Fugue.Icons.Minus);
            plus = (BitmapImage)Application.Current.FindResource(Fugue.Icons.Plus);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(20, 0);
        }

        protected override void OnTextViewChanged(TextView oldTextView, TextView newTextView)
        {
            if (oldTextView != null)
            {
                oldTextView.VisualLinesChanged -= TextViewVisualLinesChanged;
            }
            base.OnTextViewChanged(oldTextView, newTextView);
            if (newTextView != null)
            {
                newTextView.VisualLinesChanged += TextViewVisualLinesChanged;
            }
            InvalidateVisual();
        }

        void TextViewVisualLinesChanged(object? sender, EventArgs e)
        {
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (DiffLines == null)
                return;

            var textView = this.TextView;
            if (textView != null && textView.VisualLinesValid)
            {
                foreach (VisualLine line in textView.VisualLines)
                {
                    int lineNumber = line.FirstDocumentLine.LineNumber;
                    var style = DiffLines.GetLineStyle(lineNumber - 1);
                    if (style == DiffLineStyle.Added || style == DiffLineStyle.Deleted)
                    {
                        double y = line.GetTextLineVisualYPosition(line.TextLines[0], VisualYPosition.TextTop);
                        var source = style == DiffLineStyle.Added ? plus : minus;
                        drawingContext.DrawImage(source, new Rect(2, y - textView.VerticalOffset, 14, 14));
                    }
                }
            }
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }
    }
}
