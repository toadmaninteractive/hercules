using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Hercules.Forms
{
    public class TextSizeService
    {
        public TextSizeService(Typeface typeface, double emSize, double pixelsPerDip)
        {
            this.typeface = typeface;
            this.emSize = emSize;
            this.pixelsPerDip = pixelsPerDip;
        }

        private readonly double pixelsPerDip;
        private readonly Typeface typeface;
        private readonly double emSize;
        private readonly Dictionary<string, double> cache = new Dictionary<string, double>();

        public double GetWidth(string text)
        {
            if (!cache.TryGetValue(text, out var w))
            {
                w = CalcTextWidth(text);
                cache[text] = w;
            }
            return w;
        }

        private double CalcTextWidth(string text)
        {
            FormattedText formattedText = new(text, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, emSize, Brushes.Black, pixelsPerDip);
            formattedText.SetFontWeight(FontWeights.Regular);
            return formattedText.Width;
        }
    }
}
