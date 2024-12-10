using System.Collections.Generic;

namespace Hercules.Controls
{
    public class VirtualCanvasIndex
    {
        public const double AreaHeight = 250;

        private readonly List<IVirtualRow> areas = new();

        public double Height { get; private set; }

        public bool IsInitialized { get; private set; } = false;

        public void Rebuild(IVirtualRow? firstRow)
        {
            Clear();
            var row = firstRow;
            double currentAreaTop = 0;
            while (row != null)
            {
                Height = row.Top + row.Height;
                if (Height >= currentAreaTop)
                {
                    areas.Add(row);
                    currentAreaTop += AreaHeight;
                }
                row = row.Next;
            }
            IsInitialized = true;
        }

        public void Clear()
        {
            areas.Clear();
            Height = 0;
            IsInitialized = false;
        }

        public IEnumerable<IVirtualRow> GetRowsInside(Range range)
        {
            if (areas.Count == 0)
                yield break;

            var index = (int)(range.Top / AreaHeight);
            if (index > areas.Count - 1)
                index = areas.Count - 1;
            if (index < 0)
                index = 0;
            IVirtualRow? row = areas[index];
            while (row != null)
            {
                if (row.Top + row.Height > range.Top)
                {
                    if (row.Top < range.Bottom)
                        yield return row;
                    else
                        yield break;
                }
                row = row.Next;
            }
        }
    }
}
