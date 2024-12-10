using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace Hercules
{
    public static class Numbers
    {
        public static bool IsDoubleFiniteOrNaN(object o)
        {
            return !double.IsInfinity((double)o);
        }

        public static bool IsPoint(object o)
        {
            var v = (Point)o;
            return IsDoubleFiniteOrNaN(v.X) && IsDoubleFiniteOrNaN(v.Y);
        }

        public static bool Compare(double x, double y, double epsilon)
        {
            if (x == y)
                return true;

            return Math.Abs(x - y) <= epsilon;
        }

        public static bool Compare(double x, double y)
        {
            var epsilon = 1e-14;

            if (x == y)
                return true;

            double diff = Math.Max(Math.Abs(x), Math.Abs(y)) * epsilon;
            return Math.Abs(x - y) <= diff;
        }

        public static double? ParseDouble(string str)
        {
            if (double.TryParse(str, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var d))
                return d;
            else
                return null;
        }

        public static Vector ComponentMultiply(this Vector v1, Vector v2)
        {
            return new Vector(v1.X * v2.X, v1.Y * v2.Y);
        }

        public static Vector ComponentDivide(this Vector v1, Vector scale)
        {
            return new Vector(v1.X / scale.X, v1.Y / scale.Y);
        }

        public static Point ComponentMultiply(this Point v1, Vector v2)
        {
            return new Point(v1.X * v2.X, v1.Y * v2.Y);
        }

        public static Point ComponentDivide(this Point v1, Vector scale)
        {
            return new Point(v1.X / scale.X, v1.Y / scale.Y);
        }

        public static Rect WithRelativeMargin(this Rect rect, double relativeMargin)
        {
            var xMargin = rect.Width * relativeMargin;
            var yMargin = rect.Height * relativeMargin;
            return new Rect(rect.X - xMargin, rect.Y - yMargin, rect.Width + xMargin * 2, rect.Height + yMargin * 2);
        }

        public static Rect GetBounds(this IEnumerable<Point> points)
        {
            if (!points.Any())
            {
                return Rect.Empty;
            }
            var minX = points.Min(k => k.X);
            var maxX = points.Max(k => k.X);
            var minY = points.Min(k => k.Y);
            var maxY = points.Max(k => k.Y);
            var w = maxX - minX;
            var h = maxY - minY;
            return new Rect(minX, minY, w, h);
        }
    }
}
