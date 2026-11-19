using System;
using System.Reflection;

namespace Json.Serialization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, AllowMultiple = false)]
    public class JsonSerializerAttribute : Attribute
    {
        public Type? Serializer { get; set; }
        public Type? KeySerializer { get; set; }

        public JsonSerializerAttribute()
        {
            Serializer = null;
            KeySerializer = null;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public class JsonClassAttribute : Attribute
    {
        public const MemberTypes MembersDefault = MemberTypes.Field;
        public const BindingFlags BindingDefault = BindingFlags.Instance | BindingFlags.Public;

        public bool Property { get; set; }
        public bool Field { get; set; }
        public bool Public { get; set; }
        public bool NonPublic { get; set; }
        
        public JsonClassAttribute()
        {
            Property = false;
            Field = true;
            Public = true;
            NonPublic = false;
        }

        internal MemberTypes ToMemberTypes()
        {
            if (Property && Field)
                return MemberTypes.Field | MemberTypes.Property;
            if (Property)
                return MemberTypes.Property;
            return MemberTypes.Field;
        }

        internal BindingFlags ToBindingFlags()
        {
            var res = BindingFlags.Instance;
            if (Public)
                res |= BindingFlags.Public;
            if (NonPublic)
                res |= BindingFlags.NonPublic;
            return res;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class JsonFieldAttribute : Attribute
    {
        public string? JsonName { get; set; }
        public bool Ignore { get; set; }

        public JsonFieldAttribute()
        {
            JsonName = null;
            Ignore = false;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class JsonEnumAttribute : Attribute
    {
        public string? JsonName { get; set; }

        public JsonEnumAttribute()
        {
            JsonName = null;
        }
    }
}