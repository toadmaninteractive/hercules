using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using JsonDiff;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Hercules.Controls.AvalonEdit
{
    public class DiffChunksMargin : AbstractMargin
    {
        readonly ImageSource check;
        readonly ImageSource uncheck;

        IDiffChunks? diffChunks;

        public IDiffChunks? DiffChunks
        {
            get => diffChunks;
            set
            {
                if (diffChunks != value)
                {
                    diffChunks = value;
                    InvalidateVisual();
                }
            }
        }

        public DiffChunksMargin()
        {
            uncheck = (BitmapImage)Application.Current.FindResource(Fugue.Icons.UiCheckBoxUncheck);
            check = (BitmapImage)Application.Current.FindResource(Fugue.Icons.UiCheckBox);
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
            if (DiffChunks == null)
                return;

            var textView = this.TextView;
            if (textView != null && textView.VisualLinesValid)
            {
                foreach (VisualLine line in textView.VisualLines)
                {
                    int lineNumber = line.FirstDocumentLine.LineNumber;
                    var chunk = DiffChunks.GetLineChunk(lineNumber - 1, true);
                    if (chunk != null)
                    {
                        double y = line.GetTextLineVisualYPosition(line.TextLines[0], VisualYPosition.TextTop);
                        var source = chunk.Selected ? check : uncheck;
                        drawingContext.DrawImage(source, new Rect(2, y - textView.VerticalOffset, 16, 16));
                    }
                }
            }
        }

        int GetLineFromMousePosition(MouseEventArgs e)
        {
            TextView textView = this.TextView;
            if (textView == null)
                return 0;
            VisualLine vl = textView.GetVisualLineFromVisualTop(e.GetPosition(textView).Y + textView.ScrollOffset.Y);
            if (vl == null)
                return 0;
            return vl.FirstDocumentLine.LineNumber;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (DiffChunks == null)
                return;
            var line = GetLineFromMousePosition(e);
            if (line == 0)
                return;
            var chunk = DiffChunks.GetLineChunk(line - 1, true);
            if (chunk != null)
            {
                chunk.Selected = !chunk.Selected;
                // InvalidateVisual();
            }
            e.Handled = true;
            base.OnMouseLeftButtonDown(e);
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }
    }
}
