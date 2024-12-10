using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Hercules
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ViewModelTypeAttribute : Attribute
    {
        public Type Type { get; }

        public ViewModelTypeAttribute(Type type)
        {
            Type = type;
        }
    }

    public static class ViewModelTypes
    {
        private static Lazy<Dictionary<Type, Type>>? types = null;

        public static bool TryGetViewTypeByViewModelType(Type viewType, [MaybeNullWhen(false)] out Type viewModelType)
        {
            if (types == null)
                throw new InvalidOperationException();
            return types.Value.TryGetValue(viewType, out viewModelType!);
        }

        public static void CacheTypes(Assembly assembly)
        {
            types = AsyncValue.Create(() => GetTypes(assembly));
        }

        private static Dictionary<Type, Type> GetTypes(Assembly assembly)
        {
            var result = new Dictionary<Type, Type>();
            foreach (var type in assembly.GetTypes())
            {
                var attr = type.GetCustomAttribute<ViewModelTypeAttribute>();
                if (attr != null)
                {
                    result.Add(attr.Type, type);
                }
            }

            return result;
        }
    }
}
