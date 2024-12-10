using System;

namespace Hercules.Shell
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class PropertyValidatorAttribute : Attribute
    {
        public PropertyValidatorAttribute(string property)
        {
            Property = property;
        }

        public string Property { get; }
    }
}
