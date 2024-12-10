using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Hercules.Scripting
{
    public static class ScriptingApiHelper
    {
        public static CompletionData[] GetCompletionData(Type apiType)
        {
            return apiType.GetMembers()
               .Select(m => m.GetCustomAttribute<ScriptingApiAttribute>())
               .WhereNotNull()
               .Select(a => new CompletionData(a.Name, GetDescriptionText(a)))
               .ToArray();
        }

        public static Dictionary<string, object> GetApi(object api)
        {
            return
                (from m in api.GetType().GetMembers()
                 let attr = m.GetCustomAttribute<ScriptingApiAttribute>()
                 where attr != null
                 let value = GetScriptObject(m, api)
                 select new Tuple<string, object>(attr.Name, value)).ToDictionary(t => t.Item1, t => t.Item2);
        }

        private static object? GetScriptObject(MemberInfo memberInfo, object target)
        {
            return memberInfo switch
            {
                MethodInfo methodInfo => methodInfo.CreateDelegate(target),
                PropertyInfo propertyInfo => propertyInfo.GetValue(target, null),
                _ => throw new ArgumentException("Unsupported script API type", nameof(memberInfo))
            };
        }

        private static string GetDescriptionText(ScriptingApiAttribute attr) => attr.Example == null ? attr.Description : $"{attr.Description}\nExample: {attr.Example}";
    }
}
