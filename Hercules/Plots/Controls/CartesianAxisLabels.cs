using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Hercules.Controls
{
    public class CartesianAxisLabels : CartesianRenderer
    {
        public static readonly DependencyProperty FontFamilyProperty = DependencyProperty.Register("FontFamily", typeof(FontFamily), typeof(CartesianAxisLabels), new FrameworkPropertyMetadata(SystemFonts.MessageFontFamily, OnTypefaceChanged));

        public static readonly DependencyProperty FontStyleProperty = DependencyProperty.Register("FontStyle", typeof(FontStyle), typeof(CartesianAxisLabels), new FrameworkPropertyMetadata(FontStyles.Normal, OnTypefaceChanged));

        public static readonly DependencyProperty FontWeightProperty = DependencyProperty.Register("FontWeight", typeof(FontWeight), typeof(CartesianAxisLabels), new FrameworkPropertyMetadata(FontWeights.Normal, OnTypefaceChanged));

        public static readonly DependencyProperty FontStretchProperty = DependencyProperty.Register("FontStretch", typeof(FontStretch), typeof(CartesianAxisLabels), new FrameworkPropertyMetadata(FontStretches.Normal, OnTypefaceChanged));

        public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register("FontSize", typeof(double), typeof(CartesianAxisLabels), new FrameworkPropertyMetadata((double)10));

        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register("Foreground", typeof(Brush), typeof(CartesianAxisLabels), new FrameworkPropertyMetadata(null));

        public double RangeX => rangeX;
        public double RangeY => rangeY;
        public Point MinPoint => minPoint;

        [Localizability(LocalizationCategory.Font)]
        public FontFamily FontFamily
        {
            get => (FontFamily)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public FontStyle FontStyle
        {
            get => (FontStyle)GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        public FontWeight FontWeight
        {
            get => (FontWeight)GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        public FontStretch FontStretch
        {
            get => (FontStretch)GetValue(FontStretchProperty);
            set => SetValue(FontStretchProperty, value);
        }

        [TypeConverter(typeof(FontSizeConverter)), Localizability(LocalizationCategory.None)]
        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        private static void OnTypefaceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((CartesianAxisLabels)d).typeface = null;
        }

        private Typeface? typeface;
        private double rangeX;
        private double rangeY;
        private Point minPoint = new Point(0, 0);

        private Typeface GetTypeface()
        {
            if (typeface == null)
                typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
            return typeface;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var panel = (CartesianPanel)VisualParent;
            var renderRect = new Rect(RenderSize);
            var xInterval = 100 * panel.Scale.X / Math.Pow(10, Math.Round(Math.Log10(panel.Scale.X)));
            var yInterval = 100 * panel.Scale.Y / Math.Pow(10, Math.Round(Math.Log10(panel.Scale.Y)));

            var xOffset = panel.ZeroPosition.X % xInterval;
            var yOffset = panel.ZeroPosition.Y % yInterval;
            var bottomY = renderRect.Bottom - FontSize - 5;
            var leftX = 5.0;

            while (xOffset < renderRect.Width)
            {
                var x = (xOffset - panel.ZeroPosition.X) / panel.Scale.X;
                DrawText(drawingContext, x.ToString("F", CultureInfo.InvariantCulture), new Point(xOffset + 2, bottomY));
                xOffset += xInterval;
            }

            while (yOffset < renderRect.Height)
            {
                var y = -(yOffset - panel.ZeroPosition.Y) / panel.Scale.Y;
                DrawText(drawingContext, y.ToString("F", CultureInfo.InvariantCulture), new Point(leftX, yOffset - FontSize - 2));
                yOffset += yInterval;
            }

            rangeX = renderRect.Width / panel.Scale.X;
            rangeY = renderRect.Height / panel.Scale.Y;
            minPoint = new Point(-panel.ZeroPosition.X / panel.Scale.X, -rangeY + panel.ZeroPosition.Y / panel.Scale.Y);
        }

        private void DrawText(DrawingContext ctx, string text, Point point)
        {
            var dpiInfo = VisualTreeHelper.GetDpi(this);
            var formattedText = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, GetTypeface(), FontSize, Foreground, dpiInfo.PixelsPerDip);
            ctx.DrawText(formattedText, point);
        }
    }
}
