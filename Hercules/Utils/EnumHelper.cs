using System;
using System.ComponentModel;
using System.Reflection;

namespace Hercules
{
    public static class EnumHelper
    {
        public static string GetDescription(this Enum val)
        {
            var str = val.ToString();
            var memInfo = val.GetType().GetMember(str);
            var attribute = memInfo[0].GetCustomAttribute<DescriptionAttribute>();
            return attribute != null ? attribute.Description : str;
        }
    }
}
