using Hercules.Forms.Elements;
using System;
using System.Globalization;

namespace Hercules.Forms.Presentation
{
    public class FloatEditorBehavior : NumberEditorBehavior<double, FloatElement>
    {
        public FloatEditorBehavior(VirtualRowItem item, FloatElement element) : base(item, element)
        {
        }

        protected override double DefaultValue => Element.SimpleType.Default;

        protected override string FormatValue(double value)
        {
            return value.ToString(Element.SimpleType.NumberFormat);
        }

        protected override double NextValue(double value)
        {
            var type = Element.SimpleType;
            return Math.Clamp(value + type.Step.GetValueOrDefault(), type.MinValue, type.MaxValue);
        }

        protected override double PrevValue(double value)
        {
            var type = Element.SimpleType;
            return Math.Clamp(value - type.Step.GetValueOrDefault(), type.MinValue, type.MaxValue);
        }

        protected override bool TryParseValue(string text, out double value)
        {
            return double.TryParse(text, NumberStyles.Any, Element.SimpleType.NumberFormat, out value);
        }

        protected override bool IsAllowedChar(char c)
        {
            return char.IsNumber(c) || c == '-' || c == 'E' || c == 'e' || c == '.' || c == '+';
        }
    }
}
