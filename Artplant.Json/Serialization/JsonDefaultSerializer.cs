using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Json.Serialization
{
    public interface IJsonSerializer
    {
        object Deserialize(ImmutableJson json);
        ImmutableJson Serialize(object value);
    }

    public interface IJsonKeySerializer
    {
        object DeserializeKey(string str);
        string SerializeKey(object value);
    }

    public static class JsonDefaultSerializer
    {
        #region Default Serializer Cache

        public static readonly Dictionary<Type, IJsonSerializer> Serializers;
        public static readonly Dictionary<Type, IJsonKeySerializer> KeySerializers;

        static JsonDefaultSerializer()
        {
            Serializers = new Dictionary<Type, IJsonSerializer>()
            {
                { typeof(byte), Default(JsonSerializer.Byte, (byte)0) },
                { typeof(sbyte), Default(JsonSerializer.SByte, (sbyte)0) },
                { typeof(short), Default(JsonSerializer.Short, (short)0) },
                { typeof(ushort), Default(JsonSerializer.UShort, (ushort)0) },
                { typeof(int), Default(JsonSerializer.Int, (int)0) },
                { typeof(uint), Default(JsonSerializer.UInt, (uint)0) },
                { typeof(long), Default(JsonSerializer.Long, (long)0) },
                { typeof(ulong), Default(JsonSerializer.ULong, (ulong)0) },
                { typeof(float), Default(JsonSerializer.Float, (float)0) },
                { typeof(double), Default(JsonSerializer.Double, (double)0) },
                { typeof(bool), Default(JsonSerializer.Bool, false) },
                { typeof(string), Optional(JsonSerializer.String) },
                { typeof(byte[]), Optional(JsonSerializer.Binary) },
                { typeof(ImmutableJson), JsonSerializer.Json },
                { typeof(ImmutableJsonInteger), JsonSerializer.Json },
                { typeof(ImmutableJsonDouble), JsonSerializer.Json },
                { typeof(ImmutableJsonBoolean), JsonSerializer.Json },
                { typeof(ImmutableJsonNull), JsonSerializer.Json },
                { typeof(ImmutableJsonString), JsonSerializer.Json },
                { typeof(ImmutableJsonArray), JsonSerializer.Json },
                { typeof(ImmutableJsonObject), JsonSerializer.Json },
            };

            KeySerializers = new Dictionary<Type, IJsonKeySerializer>()
            {
                { typeof(byte), JsonSerializer.Byte },
                { typeof(sbyte), JsonSerializer.SByte },
                { typeof(short), JsonSerializer.Short },
                { typeof(ushort), JsonSerializer.UShort },
                { typeof(int), JsonSerializer.Int },
                { typeof(uint), JsonSerializer.UInt },
                { typeof(long), JsonSerializer.Long },
                { typeof(ulong), JsonSerializer.ULong },
                { typeof(bool), JsonSerializer.Bool },
                { typeof(string), JsonSerializer.String },
            };
        }

        #endregion

        #region Context

        public class Context
        {
            public Dictionary<Type, IJsonSerializer> Serializers = new Dictionary<Type, IJsonSerializer>();
            public Dictionary<Type, IJsonKeySerializer> KeySerializers = new Dictionary<Type, IJsonKeySerializer>();

            public EnumBaseSerializer AddEnum(Type type)
            {
                var result = new EnumBaseSerializer(type);
                Serializers[type] = result;
                KeySerializers[type] = result;
                return result;
            }

            public void Add(Type type, IJsonSerializer serializer)
            {
                Serializers[type] = serializer;
            }

            public bool TryAddKeySerializer(Type type, [MaybeNullWhen(returnValue: false)] out IJsonKeySerializer result)
            {
                result = default!;
                if (Attribute.GetCustomAttribute(type, typeof(JsonSerializerAttribute)) is JsonSerializerAttribute attr)
                {
                    result = (IJsonKeySerializer)Activator.CreateInstance(attr.KeySerializer)!;
                    KeySerializers[type] = result;
                    return true;
                }
                return false;
            }

            public bool TryAddSerializer(Type type, [MaybeNullWhen(returnValue: false)] out IJsonSerializer result)
            {
                result = default!;
                if (Attribute.GetCustomAttribute(type, typeof(JsonSerializerAttribute)) is JsonSerializerAttribute attr)
                {
                    result = (IJsonSerializer)Activator.CreateInstance(attr.Serializer)!;
                    Serializers[type] = result;
                    return true;
                }
                return false;
            }
        }

        #endregion

        #region Enum

        public class EnumBaseSerializer : IJsonSerializer, IJsonKeySerializer
        {
            readonly Type type;

            public EnumBaseSerializer(Type type)
            {
                this.type = type;
            }

            public object Deserialize(ImmutableJson json)
            {
                return DeserializeKey(json.AsString);
            }

            public object DeserializeKey(string str)
            {
                return DeserializeEnum(type, str);
            }

            public ImmutableJson Serialize(object value)
            {
                return SerializeKey(value);
            }

            public string SerializeKey(object value)
            {
                return SerializeEnum(type, value);
            }

            public static string SerializeEnum(Type type, object obj)
            {
                var memInfo = type.GetMember(obj.ToString());
                if (memInfo != null && memInfo.Length > 0)
                {
                    var attributes = memInfo[0].GetCustomAttributes(typeof(JsonEnumAttribute), false);
                    if (attributes.Length > 0)
                        return (attributes[0] as JsonEnumAttribute).JsonName;
                }
                return obj.ToString();
            }

            public static object DeserializeEnum(Type type, string str)
            {
                var memInfo = type.GetMembers();
                foreach (var el in memInfo)
                {
                    var attributes = el.GetCustomAttributes(typeof(JsonEnumAttribute), false);
                    if (attributes.Length > 0)
                        if ((attributes[0] as JsonEnumAttribute).JsonName == str)
                            return Enum.Parse(type, el.Name);
                }

                return Enum.Parse(type, str, true);
            }
        }

        public struct EnumSerializer<T> : IJsonSerializer<T>, IJsonKeySerializer<T>
        {
            public T Deserialize(ImmutableJson json)
            {
                return DeserializeKey(json.AsString);
            }

            public ImmutableJson Serialize(T value)
            {
                return SerializeKey(value);
            }

            public T DeserializeKey(string str)
            {
                return (T)EnumBaseSerializer.DeserializeEnum(typeof(T), str);
            }

            public string SerializeKey(T value)
            {
                return EnumBaseSerializer.SerializeEnum(value.GetType(), value);
            }
        }

        #endregion

        #region Class

        public class FieldSerializer
        {
            public string JsonName { get; private set; }
            public IJsonSerializer Serializer { get; } = default!;

            private readonly MemberInfo memberInfo;

            FieldInfo? FieldInfo => memberInfo as FieldInfo;

            PropertyInfo? PropertyInfo => memberInfo as PropertyInfo;

            public FieldSerializer(MemberInfo memberInfo, Context context)
            {
                this.memberInfo = memberInfo;
                JsonName = memberInfo.Name;

                var attributes = memberInfo.GetCustomAttributes(typeof(JsonFieldAttribute), true);
                if (attributes.Length > 0)
                {
                    var attr = (JsonFieldAttribute)attributes[0];

                    if (attr.Ignore)
                        return;

                    if (attr.JsonName != null)
                        JsonName = attr.JsonName;
                }

                Serializer = FieldInfo != null ? Create(FieldInfo.FieldType, context) : Create(PropertyInfo!.PropertyType, context);
            }

            public void SetValue(object obj, object val)
            {
                if (FieldInfo != null)
                    FieldInfo.SetValue(obj, val);
                else
                    PropertyInfo!.SetValue(obj, val, null);
            }

            public object? GetValue(object obj)
            {
                if (FieldInfo != null)
                    return FieldInfo.GetValue(obj);
                else
                    return PropertyInfo!.GetValue(obj, null);
            }
        }

        public class ClassSerializer : IJsonSerializer
        {
            Type type = default!;
            List<FieldSerializer> fields = default!;

            public static ClassSerializer Create(Type type, Context context)
            {
                var memberTypes = JsonClassAttribute.MembersDefault;
                var bindingFlags = JsonClassAttribute.BindingDefault;

                if (Attribute.GetCustomAttribute(type, typeof(JsonClassAttribute)) is JsonClassAttribute attr)
                {
                    memberTypes = attr.ToMemberTypes();
                    bindingFlags = attr.ToBindingFlags();
                }

                var result = new ClassSerializer();
                context.Add(type, result);
                result.type = type;
                result.fields = new List<FieldSerializer>();

                var allMembers = type.FindMembers(memberTypes, bindingFlags, null, null);
                foreach (var member in allMembers)
                {
                    var field = new FieldSerializer(member, context);
                    if (field.Serializer != null)
                        result.fields.Add(field);
                }
                return result;
            }

            private ClassSerializer()
            {
            }

            public object Deserialize(ImmutableJson json)
            {
                if (json.Equals(ImmutableJson.Null))
                    return null;

                var jsonObj = json.AsObject;
                var obj = Activator.CreateInstance(type);
                foreach (var el in fields)
                {
                    if (jsonObj.TryGetValue(el.JsonName, out var value) && !value.IsNull)
                    {
                        var data = el.Serializer.Deserialize(value);
                        el.SetValue(obj, data);
                    }
                }

                return obj;
            }

            public ImmutableJson Serialize(object value)
            {
                if (type.IsClass && value == null)
                    return ImmutableJson.Null;

                var json = new JsonObject();
                foreach (var el in fields)
                    json[el.JsonName] = el.Serializer.Serialize(el.GetValue(value));
                return json;
            }
        }

        #endregion

        #region Collections

        public class ArrayDefaultSerializer : IJsonSerializer
        {
            IJsonSerializer itemSerializer = default!;
            Type itemType = default!;

            public static ArrayDefaultSerializer Create(Type type, Context context)
            {
                var result = new ArrayDefaultSerializer();
                context.Add(type, result);
                result.itemType = type.GetElementType();
                result.itemSerializer = JsonDefaultSerializer.Create(result.itemType, context);
                return result;
            }

            ArrayDefaultSerializer()
            {
            }

            public object Deserialize(ImmutableJson json)
            {
                var result = Array.CreateInstance(itemType, json.AsArray.Count);
                var src = json.AsArray;
                for (int i = 0; i < src.Count; ++i)
                    result.SetValue(itemSerializer.Deserialize(src[i]), i);
                return result;
            }

            public ImmutableJson Serialize(object value)
            {
                var arrayValue = (Array)value;
                var arr = new JsonArray(arrayValue.Length);
                for (int i = 0; i < arrayValue.Length; ++i)
                    arr.Add(itemSerializer.Serialize(arrayValue.GetValue(i)));
                return arr;
            }
        }

        public class ListDefaultSerializer : IJsonSerializer
        {
            Type type = default!;
            IJsonSerializer itemSerializer = default!;

            public static ListDefaultSerializer Create(Type type, Context context)
            {
                var result = new ListDefaultSerializer();
                context.Add(type, result);
                Type[] args = type.GetGenericArguments();
                result.type = type;
                result.itemSerializer = JsonDefaultSerializer.Create(args[0], context);
                return result;
            }

            ListDefaultSerializer()
            {
            }

            public object Deserialize(ImmutableJson json)
            {
                var result = (IList)Activator.CreateInstance(type, json.AsArray.Count);
                foreach (var el in json.AsArray)
                    result.Add(itemSerializer.Deserialize(el));
                return result;
            }

            public ImmutableJson Serialize(object value)
            {
                var listValue = (IList)value;
                var arr = new JsonArray(listValue.Count);
                foreach (var el in listValue)
                    arr.Add(itemSerializer.Serialize(el));
                return arr;
            }
        }

        public class DictDefaultSerializer : IJsonSerializer
        {
            Type type = default!;
            IJsonKeySerializer keySerializer = default!;
            IJsonSerializer valueSerializer = default!;

            public static DictDefaultSerializer Create(Type type, Context context)
            {
                var result = new DictDefaultSerializer();
                context.Add(type, result);
                Type[] args = type.GetGenericArguments();
                result.keySerializer = CreateKey(args[0]);
                result.valueSerializer = JsonDefaultSerializer.Create(args[1], context);
                result.type = type;
                return result;
            }

            DictDefaultSerializer()
            {
            }

            public object Deserialize(ImmutableJson json)
            {
                var result = (IDictionary)Activator.CreateInstance(type, json.AsObject.Count);
                foreach (var el in json.AsObject)
                    result.Add(keySerializer.DeserializeKey(el.Key), valueSerializer.Deserialize(el.Value));
                return result;
            }

            public ImmutableJson Serialize(object value)
            {
                var dictValue = (IDictionary)value;
                var json = new JsonObject(dictValue.Count);
                foreach (DictionaryEntry el in dictValue)
                    json.Add(keySerializer.SerializeKey(el.Key), valueSerializer.Serialize(el.Value));
                return json;
            }
        }

        #endregion

        #region Optional

        public class DefaultSerializer : IJsonSerializer
        {
            readonly object defaultValue;
            readonly IJsonSerializer valueSerializer;

            public DefaultSerializer(IJsonSerializer valueSerializer, object defaultValue)
            {
                this.defaultValue = defaultValue;
                this.valueSerializer = valueSerializer;
            }

            public object Deserialize(ImmutableJson json)
            {
                if (!json.IsNull)
                    return valueSerializer.Deserialize(json);
                return defaultValue;
            }

            public ImmutableJson Serialize(object value)
            {
                if (value == null)
                    return valueSerializer.Serialize(defaultValue);
                return valueSerializer.Serialize(value);
            }
        }

        public class OptionalSerializer : IJsonSerializer
        {
            readonly IJsonSerializer valueSerializer;

            public OptionalSerializer(IJsonSerializer valueSerializer)
            {
                this.valueSerializer = valueSerializer;
            }

            public object Deserialize(ImmutableJson json)
            {
                if (!json.IsNull)
                    return valueSerializer.Deserialize(json);
                return null;
            }

            public ImmutableJson Serialize(object value)
            {
                if (value == null)
                    return ImmutableJson.Null;
                return valueSerializer.Serialize(value);
            }
        }

        public class NullableSerializer : IJsonSerializer
        {
            Type type = default!;
            PropertyInfo property = default!;
            IJsonSerializer valueSerializer = default!;

            public static NullableSerializer Create(Type type, Context context)
            {
                var result = new NullableSerializer();
                context.Add(type, result);
                result.type = type;
                result.property = type.GetProperty("Value");
                result.valueSerializer = JsonDefaultSerializer.Create(result.property.PropertyType, context);
                return result;
            }

            NullableSerializer()
            {
            }

            public object Deserialize(ImmutableJson json)
            {
                if (!json.IsNull)
                {
                    var item = valueSerializer.Deserialize(json);
                    return Activator.CreateInstance(type, item);
                }
                return null;
            }

            public ImmutableJson Serialize(object value)
            {
                if (value == null)
                    return ImmutableJson.Null;
                var item = property.GetValue(value, null);
                return valueSerializer.Serialize(item);
            }
        }

        #endregion

        #region Type

        public class TypeSerializer : IJsonSerializer
        {
            Type type = default!;

            public static TypeSerializer Create(Type type, Context context)
            {
                var result = new TypeSerializer();
                context.Add(type, result);
                result.type = type;
                return result;
            }

            private TypeSerializer()
            {
            }

            public object Deserialize(ImmutableJson json)
            {
                var result = Activator.CreateInstance(type);
                ((IJsonSerializable)result).DeserializeJson(json);
                return result;
            }

            public ImmutableJson Serialize(object value)
            {
                return ((IJsonSerializable)value).SerializeJson();
            }
        }

        public class DefaultSerializer<T> : IJsonSerializer<T>
        {
            readonly IJsonSerializer inner;

            public DefaultSerializer()
            {
                inner = Create(typeof(T));
            }

            public T Deserialize(ImmutableJson json)
            {
                return (T)inner.Deserialize(json);
            }

            public ImmutableJson Serialize(T value)
            {
                return inner.Serialize(value);
            }
        }

        #endregion

        public static IJsonSerializer Create(Type type, Context? context = null)
        {
            if (Serializers.TryGetValue(type, out var result))
                return result;

            if (context != null && context.Serializers.TryGetValue(type, out result))
                return result;

            if (context == null)
                context = new Context();

            if (context.TryAddSerializer(type, out result))
                return result;
            else if (type.IsEnum)
                return Optional(context.AddEnum(type));
            else if (type.IsArray)
                return Optional(ArrayDefaultSerializer.Create(type, context));
            else if (type.Name.StartsWith("Nullable`1"))
                return NullableSerializer.Create(type, context);
            else if (type.GetInterface("Json.Serialization.IJsonSerializable", true) != null)
                return Optional(TypeSerializer.Create(type, context));
            else if (type.GetInterface("System.Collections.IDictionary", true) != null)
                return Optional(DictDefaultSerializer.Create(type, context));
            else if (type.GetInterface("System.Collections.IList", true) != null)
                return Optional(ListDefaultSerializer.Create(type, context));
            else if (type.IsClass)
                return Optional(ClassSerializer.Create(type, context));
            else if (type.IsValueType)
                return Optional(ClassSerializer.Create(type, context));

            throw new Exception("Type " + type + " doesn't have default IJsonSerializer.");
        }

        public static IJsonKeySerializer CreateKey(Type type, Context? context = null)
        {
            if (KeySerializers.TryGetValue(type, out var result))
                return result;

            if (context != null && context.KeySerializers.TryGetValue(type, out result))
                return result;

            if (context == null)
                context = new Context();

            if (context.TryAddKeySerializer(type, out result))
                return result;
            else if (type.IsEnum)
                return context.AddEnum(type);

            throw new Exception("Type " + type + " doesn't have default IJsonKeySerializer.");
        }

        public static IJsonSerializer<T> Create<T>()
        {
            return new DefaultSerializer<T>();
        }

        public static IJsonSerializer<T> EnumValue<T>()
        {
            return new EnumSerializer<T>();
        }

        public static IJsonSerializer Default(IJsonSerializer valueSerializer, object defaultValue)
        {
            return new DefaultSerializer(valueSerializer, defaultValue);
        }

        public static IJsonSerializer Optional(IJsonSerializer valueSerializer)
        {
            return new OptionalSerializer(valueSerializer);
        }
    }
}
