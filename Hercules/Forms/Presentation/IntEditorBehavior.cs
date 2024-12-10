using Hercules.Forms.Elements;
using System;
using System.Globalization;

namespace Hercules.Forms.Presentation
{
    public class IntEditorBehavior : NumberEditorBehavior<int, IntElement>
    {
        public IntEditorBehavior(VirtualRowItem item, IntElement element) : base(item, element)
        {
        }

        protected override int DefaultValue => Element.SimpleType.Default;

        protected override string FormatValue(int value)
        {
            return value.ToString(Element.SimpleType.NumberFormat);
        }

        protected override int NextValue(int value)
        {
            var type = Element.SimpleType;
            return Math.Clamp(value + type.Step, type.MinValue, type.MaxValue);
        }

        protected override int PrevValue(int value)
        {
            var type = Element.SimpleType;
            return Math.Clamp(value - type.Step, type.MinValue, type.MaxValue);
        }

        protected override bool TryParseValue(string text, out int value)
        {
            return int.TryParse(text, NumberStyles.AllowLeadingSign, Element.SimpleType.NumberFormat, out value);
        }

        protected override bool IsAllowedChar(char c)
        {
            return char.IsNumber(c) || c == '-';
        }
    }
}
