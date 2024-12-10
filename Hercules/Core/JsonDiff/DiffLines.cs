using System;
using System.Collections.Generic;

namespace JsonDiff
{
    public enum DiffLineStyle
    {
        Normal,
        Added,
        Deleted,
        Skip,
    }

    public interface IDiffLines
    {
        DiffLineStyle GetLineStyle(int lineIndex);
    }

    public class DiffLines : IDiffLines
    {
        class EmptyDiffLines : IDiffLines
        {
            public DiffLineStyle GetLineStyle(int lineIndex)
            {
                return DiffLineStyle.Normal;
            }
        }

        public static readonly IDiffLines Default = new EmptyDiffLines();

        public List<DiffLineStyle> Lines { get; }

        public DiffLines(List<DiffLineStyle> lines)
        {
            this.Lines = lines;
        }

        public DiffLineStyle GetLineStyle(int lineIndex)
        {
            if (lineIndex < Lines.Count)
                return Lines[lineIndex];
            else
                return DiffLineStyle.Normal;
        }

        public int NextDifference(int i)
        {
            int j = Math.Max(i, 0);
            while (j < Lines.Count && Lines[j] != DiffLineStyle.Normal)
                j++;
            while (j < Lines.Count && Lines[j] == DiffLineStyle.Normal)
                j++;
            if (j >= Lines.Count)
            {
                j = 0;
                while (j < i && Lines[j] == DiffLineStyle.Normal)
                    j++;
                if (j == i)
                    return i;
            }
            return j + 1;
        }

        public int PreviousDifference(int i)
        {
            int j = Math.Min(i - 1, Lines.Count - 1);
            while (j >= 0 && Lines[j] != DiffLineStyle.Normal)
                j--;
            while (j >= 0 && Lines[j] == DiffLineStyle.Normal)
                j--;
            if (j < 0)
            {
                j = Lines.Count - 1;
                while (j > i && Lines[j] == DiffLineStyle.Normal)
                    j--;
                if (j == i)
                    return i;
            }
            return j + 1;
        }
    }
}
