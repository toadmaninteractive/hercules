using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace Hercules.Scripting
{
    public class CompletionDataList
    {
        public IReadOnlyList<ICompletionData> Data => data;
        private readonly List<ICompletionData> data = new(16);
        private readonly Dictionary<string, ICompletionData> textToData = new();

        public void Add(ICompletionData entry)
        {
            if (!textToData.ContainsKey(entry.Text))
            {
                textToData.Add(entry.Text, entry);
                data.Add(entry);
            }
        }

        public void AddRange(IEnumerable<ICompletionData> entries)
        {
            foreach (var entry in entries)
            {
                Add(entry);
            }
        }

        public void Sort()
        {
            data.Sort((c1, c2) => string.Compare(c1.Text, c2.Text, StringComparison.CurrentCulture));
        }
    }

    public class CompletionData : ICompletionData
    {
        public CompletionData(string text, string description)
        {
            this.Text = text;
            this.description = description;
        }

        private readonly string description;

        public ImageSource? Image => null;

        public string Text { get; }

        // Use this property if you want to show a fancy UIElement in the list.
        public object Content => Text;

        public object Description => description;

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, this.Text);
        }

        public double Priority => 0;
    }
}
