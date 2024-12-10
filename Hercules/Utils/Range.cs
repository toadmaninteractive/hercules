using System;

namespace Hercules
{
    public readonly record struct Range(double Top, double Height)
    {
        public double Bottom => Top + Height;

        public bool IntersectsWith(Range other)
        {
            return other.Top < Bottom && Top < other.Bottom;
        }

        public static readonly Range Empty = new(0, 0);

        public static Range Intersect(Range a, Range b)
        {
            if (!a.IntersectsWith(b))
                return Range.Empty;
            return new Range(Math.Max(a.Top, b.Top), Math.Min(a.Bottom, b.Bottom));
        }

        public static Range Union(Range a, Range b)
        {
            return new Range(Math.Min(a.Top, b.Top), Math.Max(a.Bottom, b.Bottom));
        }
    }
}
