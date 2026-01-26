using ICSharpCode.AvalonEdit;
using Microsoft.Xaml.Behaviors;
using System;
using System.Windows;

namespace Hercules.Controls
{
    public class AvalonEditSearchBehavior : Behavior<TextEditor>, ISearchTarget
    {
        public static readonly DependencyProperty SearchTargetProperty = DependencyProperty.Register(nameof(SearchTarget), typeof(ISearchTarget), typeof(AvalonEditSearchBehavior));

        public ISearchTarget SearchTarget
        {
            get => this;
            set => throw new InvalidOperationException();
        }

        protected override void OnAttached()
        {
            SetValue(SearchTargetProperty, this);
        }

        protected override void OnDetaching()
        {
            SetValue(SearchTargetProperty, null);
        }

        private int currentIndex;
        private string? selectionText;

        public void FindNext(string searchText, SearchDirection searchDirection, bool matchCase, bool wholeWord)
        {
            if (selectionText != searchText)
            {
                currentIndex = -1;
                selectionText = searchText;
            }

            var matchIndexList = SearchHelper.GetMatchPositionList(AssociatedObject.Document.Text, searchText, matchCase, wholeWord);

            if (matchIndexList.Count == 0)
            {
                AssociatedObject.SelectionStart = 0;
                AssociatedObject.SelectionLength = 0;
                return;
            }

            switch (searchDirection)
            {
                case SearchDirection.Next:
                    currentIndex = currentIndex < matchIndexList.Count - 1 ? currentIndex + 1 : 0;
                    AssociatedObject.SelectionStart = matchIndexList[currentIndex].IndexNumber;
                    break;
                case SearchDirection.Previous:
                    currentIndex = currentIndex > 0 ? currentIndex - 1 : matchIndexList.Count - 1;
                    AssociatedObject.SelectionStart = matchIndexList[currentIndex].IndexNumber;
                    break;
            }

            AssociatedObject.SelectionLength = searchText.Length;
            AssociatedObject.ScrollToLine(matchIndexList[currentIndex].LineNumber);
        }
    }
}