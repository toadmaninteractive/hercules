using Hercules.Controls.AvalonEdit;
using ICSharpCode.AvalonEdit;
using Microsoft.Xaml.Behaviors;
using System;

namespace Hercules.Controls
{
    public class HighlightSelectedIdentifierBehaviour : Behavior<TextEditor>
    {
        HighlightSelectedIdentifierColorizer? colorizer;

        protected override void OnAttached()
        {
            InstallColorizer();
            AssociatedObject.TextArea.SelectionChanged += SelectionChanged;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.TextArea.SelectionChanged -= SelectionChanged;
            UninstallColorizer();
        }

        void SelectionChanged(object? sender, EventArgs args)
        {
            AssociatedObject.TextArea.TextView.Redraw();
        }

        void InstallColorizer()
        {
            if (colorizer == null)
            {
                colorizer = new HighlightSelectedIdentifierColorizer(AssociatedObject);
                AssociatedObject.TextArea.TextView.LineTransformers.Add(colorizer);
            }
        }

        void UninstallColorizer()
        {
            if (colorizer != null)
            {
                AssociatedObject.TextArea.TextView.LineTransformers.Remove(colorizer);
                colorizer = null;
            }
        }
    }
}
