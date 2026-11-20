using EnumsNET;
using System;
using System.Linq;
using System.Windows.Markup;

namespace Hercules.Controls
{
    public class EnumDescriptions : MarkupExtension
    {
        internal class EnumValue<T> where T : struct, Enum
        {
            public T Value { get; }
            public string? Description { get; }

            public EnumValue(T val)
            {
                this.Value = val;
                this.Description = val.AsString(EnumFormat.Description) ?? val.AsString(EnumFormat.Name);
            }

            public override string? ToString() => Description;

            public static implicit operator T(EnumValue<T> val) => val.Value;
        }

        private readonly Type type;
        private readonly Type enumValueType;

        public EnumDescriptions(Type type)
        {
            this.type = type;
            this.enumValueType = typeof(EnumValue<>).MakeGenericType(type);
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Enum.GetValues(type).Cast<object>().Select(e => Activator.CreateInstance(enumValueType, e));
        }
    }
}
