using Hercules.Scripting;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using Microsoft.Xaml.Behaviors;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Telerik.Windows.Diagrams.Core;

namespace Hercules.Controls
{
    public class CodeCompletionBehavior : Behavior<TextEditor>
    {
        public static readonly DependencyProperty StrategyProperty = DependencyProperty.Register(nameof(Strategy), typeof(ICodeCompletionStrategy), typeof(CodeCompletionBehavior));

        public ICodeCompletionStrategy? Strategy
        {
            get => (ICodeCompletionStrategy)GetValue(StrategyProperty);
            set => SetValue(StrategyProperty, value);
        }

        protected override void OnAttached()
        {
            AssociatedObject.TextArea.TextEntering += TextEditor_TextArea_TextEntering;
            AssociatedObject.TextArea.TextEntered += TextEditor_TextArea_TextEntered;
            AssociatedObject.PreviewKeyDown += TextEditor_PreviewKeyDown;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.TextArea.TextEntering -= TextEditor_TextArea_TextEntering;
            AssociatedObject.TextArea.TextEntered -= TextEditor_TextArea_TextEntered;
            AssociatedObject.PreviewKeyDown -= TextEditor_PreviewKeyDown;
        }

        CompletionWindow? completionWindow;

        void TextEditor_TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            if (e.Text is "." or "\"" or "'")
                SuggestCompletion();
        }

        void TextEditor_TextArea_TextEntering(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0 && completionWindow != null)
            {
                var c = e.Text[0];
                if (!char.IsLetterOrDigit(c) && c != '_')
                {
                    completionWindow.Close();
                }
            }
        }

        void ShowCompletionWindow(IEnumerable<ICompletionData> fields, string? prefix = null)
        {
            completionWindow = new CompletionWindow(AssociatedObject.TextArea) { Width = 250 };
            completionWindow.Closed += (sender, e) => completionWindow = null;
            completionWindow.CompletionList.CompletionData.AddRange(fields);
            if (prefix != null)
            {
                completionWindow.StartOffset = AssociatedObject.CaretOffset - prefix.Length;
                completionWindow.CompletionList.SelectItem(prefix);
            }

            completionWindow.Show();
        }

        void SuggestCompletion()
        {
            if (completionWindow != null)
                return;

            var strategy = Strategy;
            if (strategy == null)
                return;

            if (AssociatedObject.CaretOffset < 0)
                return;

            var completion = new CompletionDataList();
            if (strategy.SuggestCompletion(AssociatedObject.Document, AssociatedObject.CaretOffset, completion, out var prefix))
            {
                ShowCompletionWindow(completion.Data, prefix);
            }
        }

        private void TextEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                SuggestCompletion();
                e.Handled = true;
            }
        }
    }
}
