using System;
using System.Collections.Generic;
using System.Linq;

namespace Hercules
{
    public enum SearchDirection
    {
        Next,
        Previous
    }

    public interface ISearchTarget
    {
        void FindNext(string searchText, SearchDirection searchDirection, bool matchCase, bool wholeWord);
    }

    public readonly record struct MatchPosition(int IndexNumber, int LineNumber);

    public static class SearchHelper
    {
        public static bool MatchString(string value, string pattern, bool matchCase, bool wholeWord)
        {
            var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            var index = value.IndexOf(pattern, comparison);
            if (index < 0)
                return false;
            if (wholeWord)
            {
                if (index > 0)
                {
                    char c = value[index - 1];
                    if (char.IsLetter(c) || c == '_')
                        return false;
                }
                if (index + pattern.Length < value.Length)
                {
                    char c = value[index + pattern.Length];
                    if (char.IsLetterOrDigit(c) || c == '_')
                        return false;
                }
            }
            return true;
        }

        public static List<MatchPosition> GetMatchPositionList(string allText, string pattern, bool matchCase, bool wholeWord)
        {
            var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            var matchIndexList = new List<MatchPosition>();
            int matchIndex;
            int startIndex = 0;
            do
            {
                matchIndex = allText.IndexOf(pattern, startIndex, comparison);
                if (matchIndex < 0)
                    break;

                var substring = allText.Substring(0, matchIndex);
                var lineNumber = System.Text.RegularExpressions.Regex.Matches(substring, "\r\n").Count;

                matchIndexList.Add(new MatchPosition(matchIndex, lineNumber));
                startIndex = matchIndex + pattern.Length;
            } while (matchIndex >= 0);

            if (!wholeWord)
                return matchIndexList;

            //============

            char[] brackets = { '/', '\"' };
            var wholeWordMatchIndexList = new List<MatchPosition>();
            foreach (var matchIndexCandidate in matchIndexList)
            {
                if (matchIndexCandidate.IndexNumber == 0)
                    continue;

                if (brackets.Contains(allText[matchIndexCandidate.IndexNumber - 1]) && brackets.Contains(allText[matchIndexCandidate.IndexNumber + pattern.Length]))
                    wholeWordMatchIndexList.Add(matchIndexCandidate);
            }

            return wholeWordMatchIndexList;
        }

        public static bool MatchNumber(string value, double? number)
        {
            if (!number.HasValue)
                return false;
            var d = Numbers.ParseDouble(value);
            if (d.HasValue)
                return Numbers.Compare(d.Value, number.Value);
            else
                return false;
        }
    }
}
