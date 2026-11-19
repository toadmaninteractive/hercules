using System;
using System.Collections.Generic;
using System.Globalization;

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

    public interface IJsonSerializable
    {
        ImmutableJson SerializeJson();
        void DeserializeJson(ImmutableJson json);
    }

    public interface IJsonSerializer<T>
    {
        T Deserialize(ImmutableJson json);
        ImmutableJson Serialize(T value);
    }

    public interface IJsonKeySerializer<T>
    {
        T DeserializeKey(string str);
        string SerializeKey(T value);
    }

    public static class JsonSerializer
    {
        #region Numbers

        public class ByteSerializer : IJsonSerializer<byte>, IJsonKeySerializer<byte>, IJsonSerializer, IJsonKeySerializer
        {
            public byte Deserialize(ImmutableJson json)
            {
                return (byte)json.AsInt;
            }

            public ImmutableJson Serialize(byte value)
            {
                return value;
            }

            public byte DeserializeKey(string str)
            {
                return byte.Parse(str, CultureInfo.InvariantCulture);
            }

            public string SerializeKey(byte value)
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }

            object IJsonSerializer.Deserialize(ImmutableJson json)
            {
                return Deserialize(json);
            }

            ImmutableJson IJsonSerializer.Serialize(object value)
            {
                return Serialize((byte)value);
            }

            object IJsonKeySerializer.DeserializeKey(string str)
            {
                return DeserializeKey(str);
            }

            string IJsonKeySerializer.SerializeKey(object value)
            {
                return SerializeKey((byte)value);
            }
        }

        public class SByteSerializer : IJsonSerializer<sbyte>, IJsonKeySerializer<sbyte>, IJsonSerializer, IJsonKeySerializer
        {
            public sbyte Deserialize(ImmutableJson json)
            {
                return (sbyte)json.AsInt;
            }

            public ImmutableJson Serialize(sbyte value)
            {
                return value;
            }

            public sbyte DeserializeKey(string str)
            {
                return sbyte.Parse(str, CultureInfo.InvariantCulture);
            }

            public string SerializeKey(sbyte value)
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }

            object IJsonSerializer.Deserialize(ImmutableJson json)
            {
                return Deserialize(json);
            }

            ImmutableJson IJsonSerializer.Serialize(object value)
            {
                return Serialize((sbyte)value);
            }

            object IJsonKeySerializer.DeserializeKey(string str)
            {
                return DeserializeKey(str);
            }

            string IJsonKeySerializer.SerializeKey(object value)
            {
                return SerializeKey((sbyte)value);
            }
        }

        public class ShortSerializer : IJsonSerializer<short>, IJsonKeySerializer<short>, IJsonSerializer, IJsonKeySerializer
        {
            public short Deserialize(ImmutableJson json)
            {
                return (short)json.AsInt;
            }

            public ImmutableJson Serialize(short value)
            {
                return value;
            }

            public short DeserializeKey(string str)
            {
                return short.Parse(str, CultureInfo.InvariantCulture);
            }

            public string SerializeKey(short value)
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }

            object IJsonSerializer.Deserialize(ImmutableJson json)
            {
                return Deserialize(json);
            }

            ImmutableJson IJsonSerializer.Serialize(object value)
            {
                return Serialize((short)value);
            }

            object IJsonKeySerializer.DeserializeKey(string str)
            {
                return DeserializeKey(str);
            }

            string IJsonKeySerializer.SerializeKey(object value)
            {
                return SerializeKey((short)value);
            }
        }

        public class UShortSerializer : IJsonSerializer<ushort>, IJsonKeySerializer<ushort>, IJsonSerializer, IJsonKeySerializer
        {
            public ushort Deserialize(ImmutableJson json)
            {
                return (ushort)json.AsInt;
            }

            public ImmutableJson Serialize(ushort value)
            {
                return value;
            }

            public ushort DeserializeKey(string str)
            {
                return ushort.Parse(str, CultureInfo.InvariantCulture);
            }

            public string SerializeKey(ushort value)
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }

            object IJsonSerializer.Deserialize(ImmutableJson json)
            {
                return Deserialize(json);
            }

            ImmutableJson IJsonSerializer.Serialize(object value)
            {
                return Serialize((ushort)value);
            }

            object IJsonKeySerializer.DeserializeKey(string str)
            {
                return DeserializeKey(str);
            }

            string IJsonKeySerializer.SerializeKey(object value)
            {
                return SerializeKey((ushort)value);
            }
        }

        public class IntSerializer : IJsonSerializer<int>, IJsonKeySerializer<int>, IJsonSerializer, IJsonKeySerializer
        {
            public int Deserialize(ImmutableJson json)
            {
                return json.AsInt;
            }

            public ImmutableJson Serialize(int value)
            {
                return value;
            }

            public int DeserializeKey(string str)
            {
                return int.Parse(str, CultureInfo.InvariantCulture);
            }

            public string SerializeKey(int value)
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }
            object IJsonSerializer.Deserialize(ImmutableJson json)
            {
                return Deserialize(json);
            }

            ImmutableJson IJsonSerializer.Serialize(object value)
            {
                return Serialize((int)value);
            }

            object IJsonKeySerializer.DeserializeKey(string str)
            {
                return DeserializeKey(str);
            }

            string IJsonKeySerializer.SerializeKey(object value)
            {
                return SerializeKey((int)value);
            }
        }

        public class UIntSerializer : IJsonSerializer<uint>, IJsonKeySerializer<uint>, IJsonSerializer, IJsonKeySerializer
        {
            public uint Deserialize(ImmutableJson json)
            {
                return (uint)json.AsInt;
            }

            public ImmutableJson Serialize(uint value)
            {
                return (int)value;
            }

            public uint DeserializeKey(string str)
            {
                return uint.Parse(str, CultureInfo.InvariantCulture);
            }

            public string SerializeKey(uint value)
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }

            object IJsonSerializer.Deserialize(ImmutableJson json)
            {
                return Deserialize(json);
            }

            ImmutableJson IJsonSerializer.Serialize(object value)
            {
                return Serialize((uint)value);
            }

            object IJsonKeySerializer.DeserializeKey(string str)
            {
                return DeserializeKey(str);
            }

            string IJsonKeySerializer.SerializeKey(object value)
            {
                return SerializeKey((uint)value);
            }
        }

        public class LongSerializer : IJsonSerializer<long>, IJsonKeySerializer<long>, IJsonSerializer, IJsonKeySerializer
        {
            public long Deserialize(ImmutableJson json)
            {
                return json.AsLong;
            }

            public ImmutableJson Serialize(long value)
            {
                return value;
            }

            public long DeserializeKey(string str)
            {
                return long.Parse(str, CultureInfo.InvariantCulture);
            }

            public string SerializeKey(long value)
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }

            object IJsonSerializer.Deserialize(ImmutableJson json)
            {
                return Deserialize(json);
            }

            ImmutableJson IJsonSerializer.Serialize(object value)
            {
                return Serialize((byte)value);
            }

            object IJsonKeySerializer.DeserializeKey(string str)
            {
                return DeserializeKey(str);
            }

            string IJsonKeySerializer.SerializeKey(object value)
            {
                return SerializeKey((byte)value);
            }
        }

        public class ULongSerializer : IJsonSerializer<ulong>, IJsonKeySerializer<ulong>, IJsonSerializer, IJsonKeySerializer
        {
            public ulong Deserialize(ImmutableJson json)
            {
                return (ulong)json.AsLong;
            }

            public ImmutableJson Serialize(ulong value)
            {
                return (long)value;
            }

            public ulong DeserializeKey(string str)
            {
                return ulong.Parse(str, CultureInfo.InvariantCulture);
            }

            public string SerializeKey(ulong value)
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }

            object IJsonSerializer.Deserialize(ImmutableJson json)
            {
                return Deserialize(json);
            }

            ImmutableJson IJsonSerializer.Serialize(object value)
            {
                return Serialize((ulong)value);
            }

            object IJsonKeySerializer.DeserializeKey(string str)
            {
                return DeserializeKey(str);
            }

            string IJsonKeySerializer.SerializeKey(object value)
            {
                return SerializeKey((ulong)value);
            }
        }

        public class FloatSerializer : IJsonSerializer<float>, IJsonSerializer
        {
            public float Deserialize(ImmutableJson json)
            {
                return (float)json.AsNumber;
            }

            public ImmutableJson Serialize(float value)
            {
                return value;
            }

            object IJsonSerializer.Deserialize(ImmutableJson json)
            {
                return Deserialize(json);
            }

            ImmutableJson IJsonSerializer.Serialize(object value)
            {
                return Serialize((float)value);
            }
        }

        public class DoubleSerializer : IJsonSerializer<double>, IJsonSerializer
        {
            public double Deserialize(ImmutableJson json)
            {
                return json.AsNumber;
            }

            public ImmutableJson Serialize(double value)
            {
                return value;
            }

            object IJsonSerializer.Deserialize(ImmutableJson json)
            {
                return Deserialize(json);
            }

            ImmutableJson IJsonSerializer.Serialize(object value)
            {
                return Serialize((double)value);
            }
        }

        #endregion

        #region Boolean

        public class BoolSerializer : IJsonSerializer<bool>, IJsonKeySerializer<bool>, IJsonSerializer, IJsonKeySerializer
        {
            public bool Deserialize(ImmutableJson json)
            {
                return json.AsBool;
            }

            public ImmutableJson Serialize(bool value)
            {
                return value;
            }

            public bool DeserializeKey(string str)
            {
                if (str == "true")
                    return true;
                if (str == "false")
                    return false;
                throw new FormatException("Boolean format was invalid: " + str);
            }

            public string SerializeKey(bool value)
            {
                return value ? "true" : "false";
            }

            object IJsonSerializer.Deserialize(ImmutableJson json)
            {
                return Deserialize(json);
            }

            ImmutableJson IJsonSerializer.Serialize(object value)
            {
                return Serialize((bool)value);
            }

            object IJsonKeySerializer.DeserializeKey(string str)
            {
                return DeserializeKey(str);
            }

            string IJsonKeySerializer.SerializeKey(object value)
            {
                return SerializeKey((bool)value);
            }
        }

        #endregion

        #region String

        public class StringSerializer : IJsonSerializer<string>, IJsonKeySerializer<string>, IJsonSerializer, IJsonKeySerializer
        {
            public string Deserialize(ImmutableJson json)
            {
                return json.AsString;
            }

            public ImmutableJson Serialize(string value)
            {
                return value;
            }

            public string DeserializeKey(string str)
            {
                return str;
            }

            public string SerializeKey(string value)
            {
                return value;
            }

            object IJsonSerializer.Deserialize(ImmutableJson json)
            {
                return Deserialize(json);
            }

            ImmutableJson IJsonSerializer.Serialize(object value)
            {
                return Serialize((string)value);
            }

            object IJsonKeySerializer.DeserializeKey(string str)
            {
                return DeserializeKey(str);
            }

            string IJsonKeySerializer.SerializeKey(object value)
            {
                return SerializeKey((string)value);
            }
        }

        #endregion

        #region Binary

        public class BinarySerializer : IJsonSerializer<byte[]>, IJsonSerializer
        {
            public byte[] Deserialize(ImmutableJson json)
            {
                return Convert.FromBase64String(json.AsString);
            }

            public ImmutableJson Serialize(byte[] value)
            {
                return Convert.ToBase64String(value);
            }

            object IJsonSerializer.Deserialize(ImmutableJson json)
            {
                return Deserialize(json);
            }

            ImmutableJson IJsonSerializer.Serialize(object value)
            {
                return Serialize((byte[])value);
            }
        }

        #endregion

        #region JsonValue

        public class JsonValueSerializer : IJsonSerializer<ImmutableJson>, IJsonSerializer
        {
            public ImmutableJson Deserialize(ImmutableJson json)
            {
                return json;
            }

            public ImmutableJson Serialize(ImmutableJson value)
            {
                return value;
            }

            object IJsonSerializer.Deserialize(ImmutableJson json)
            {
                return Deserialize(json);
            }

            ImmutableJson IJsonSerializer.Serialize(object value)
            {
                return Serialize((ImmutableJson)value);
            }
        }

        #endregion

        #region Optional

        public struct NullableSerializer<T> : IJsonSerializer<T?> where T : struct
        {
            readonly IJsonSerializer<T> valueSerializer;

            public NullableSerializer(IJsonSerializer<T> valueSerializer)
            {
                this.valueSerializer = valueSerializer;
            }

            public T? Deserialize(ImmutableJson json)
            {
                if (json.IsNull)
                    return null;
                return valueSerializer.Deserialize(json);
            }

            public ImmutableJson Serialize(T? value)
            {
                if (value.HasValue)
                    return valueSerializer.Serialize(value.Value);
                return ImmutableJson.Null;
            }
        }

        public struct OptionalSerializer<T> : IJsonSerializer<T?> where T : class
        {
            readonly IJsonSerializer<T> valueSerializer;

            public OptionalSerializer(IJsonSerializer<T> valueSerializer)
            {
                this.valueSerializer = valueSerializer;
            }

            public T? Deserialize(ImmutableJson json)
            {
                if (json.IsNull)
                    return null;
                return valueSerializer.Deserialize(json);
            }

            public ImmutableJson Serialize(T? value)
            {
                if (value != null)
                    return valueSerializer.Serialize(value);
                return ImmutableJson.Null;
            }
        }

        #endregion

        #region Collections

        public struct ArraySerializer<T> : IJsonSerializer<T[]>
        {
            readonly IJsonSerializer<T> itemSerializer;

            public ArraySerializer(IJsonSerializer<T> itemSerializer)
            {
                this.itemSerializer = itemSerializer;
            }

            public T[] Deserialize(ImmutableJson json)
            {
                var source = json.AsArray;
                var result = new T[source.Count];
                for (int i = 0; i < source.Count; ++i)
                    result[i] = itemSerializer.Deserialize(source[i]);
                return result;
            }

            public ImmutableJson Serialize(T[] value)
            {
                var arr = new JsonArray(value.Length);
                foreach (var el in value)
                    arr.Add(itemSerializer.Serialize(el));
                return arr;
            }
        }

        public struct ListSerializer<T> : IJsonSerializer<List<T>>
        {
            readonly IJsonSerializer<T> itemSerializer;

            public ListSerializer(IJsonSerializer<T> itemSerializer)
            {
                this.itemSerializer = itemSerializer;
            }

            public List<T> Deserialize(ImmutableJson json)
            {
                var result = new List<T>(json.AsArray.Count);
                foreach (var el in json.AsArray)
                    result.Add(itemSerializer.Deserialize(el));
                return result;
            }

            public ImmutableJson Serialize(List<T> value)
            {
                if (value.Count == 0)
                    return ImmutableJson.EmptyArray;
                var arr = new JsonArray(value.Count);
                foreach (var el in value)
                    arr.Add(itemSerializer.Serialize(el));
                return arr;
            }
        }

        public struct ReadOnlyListSerializer<T> : IJsonSerializer<IReadOnlyList<T>>
        {
            readonly IJsonSerializer<T> itemSerializer;

            public ReadOnlyListSerializer(IJsonSerializer<T> itemSerializer)
            {
                this.itemSerializer = itemSerializer;
            }

            public IReadOnlyList<T> Deserialize(ImmutableJson json)
            {
                var jsonArray = json.AsArray;
#if NETCOREAPP3_0 || NETSTANDARD2_0
                if (jsonArray.Count == 0)
                    return Array.Empty<T>();
#endif
                var result = new T[json.AsArray.Count];
                for (int i = 0; i < jsonArray.Count; i++)
                    result[i] = itemSerializer.Deserialize(jsonArray[i]);
                return result;
            }

            public ImmutableJson Serialize(IReadOnlyList<T> value)
            {
                if (value.Count == 0)
                    return ImmutableJson.EmptyArray;
                var arr = new JsonArray(value.Count);
                foreach (var el in value)
                    arr.Add(itemSerializer.Serialize(el));
                return arr;
            }
        }

        public struct DictSerializer<TKey, TValue> : IJsonSerializer<Dictionary<TKey, TValue>> where TKey : notnull
        {
            readonly IJsonKeySerializer<TKey> keySerializer;
            readonly IJsonSerializer<TValue> valueSerializer;
            readonly IEqualityComparer<TKey>? equalityComparer;

            public DictSerializer(IJsonKeySerializer<TKey> keySerializer, IJsonSerializer<TValue> valueSerializer)
                : this(keySerializer, valueSerializer, null)
            {
            }

            public DictSerializer(IJsonKeySerializer<TKey> keySerializer, IJsonSerializer<TValue> valueSerializer, IEqualityComparer<TKey>? equalityComparer)
            {
                this.keySerializer = keySerializer;
                this.valueSerializer = valueSerializer;
                this.equalityComparer = equalityComparer;
            }

            public Dictionary<TKey, TValue> Deserialize(ImmutableJson json)
            {
                var jsonObject = json.AsObject;
                var result = new Dictionary<TKey, TValue>(jsonObject.Count, equalityComparer ?? EqualityComparer<TKey>.Default);
                foreach (var el in jsonObject)
                    result.Add(keySerializer.DeserializeKey(el.Key), valueSerializer.Deserialize(el.Value));
                return result;
            }

            public ImmutableJson Serialize(Dictionary<TKey, TValue> value)
            {
                if (value.Count == 0)
                    return ImmutableJson.EmptyObject;
                var json = new JsonObject(value.Count);
                foreach (var el in value)
                    json.Add(keySerializer.SerializeKey(el.Key), valueSerializer.Serialize(el.Value));
                return json;
            }
        }

        public struct ReadOnlyDictSerializer<TKey, TValue> : IJsonSerializer<IReadOnlyDictionary<TKey, TValue>> where TKey : notnull
        {
            readonly IJsonKeySerializer<TKey> keySerializer;
            readonly IJsonSerializer<TValue> valueSerializer;
            readonly IEqualityComparer<TKey>? equalityComparer;

            public ReadOnlyDictSerializer(IJsonKeySerializer<TKey> keySerializer, IJsonSerializer<TValue> valueSerializer)
                : this(keySerializer, valueSerializer, null)
            {
            }

            public ReadOnlyDictSerializer(IJsonKeySerializer<TKey> keySerializer, IJsonSerializer<TValue> valueSerializer, IEqualityComparer<TKey>? equalityComparer)
            {
                this.keySerializer = keySerializer;
                this.valueSerializer = valueSerializer;
                this.equalityComparer = equalityComparer;
            }

            public IReadOnlyDictionary<TKey, TValue> Deserialize(ImmutableJson json)
            {
                var jsonObject = json.AsObject;
#if NETCOREAPP3_0
                if (jsonObject.Count == 0)
                    return System.Collections.Immutable.ImmutableDictionary<TKey, TValue>.Empty;
#endif
                var result = new Dictionary<TKey, TValue>(jsonObject.Count, equalityComparer ?? EqualityComparer<TKey>.Default);
                foreach (var el in jsonObject)
                    result.Add(keySerializer.DeserializeKey(el.Key), valueSerializer.Deserialize(el.Value));
                return result;
            }

            public ImmutableJson Serialize(IReadOnlyDictionary<TKey, TValue> value)
            {
                if (value.Count == 0)
                    return ImmutableJson.EmptyObject;
                var json = new JsonObject(value.Count);
                foreach (var el in value)
                    json.Add(keySerializer.SerializeKey(el.Key), valueSerializer.Serialize(el.Value));
                return json;
            }
        }

#endregion

#region Enums

        public struct EnumFlagsSerializer<T> : IJsonSerializer<T> where T : Enum
        {
            readonly IJsonSerializer<T> itemSerializer;

            public EnumFlagsSerializer(IJsonSerializer<T> itemSerializer)
            {
                this.itemSerializer = itemSerializer;
            }

            public T Deserialize(ImmutableJson json)
            {
                int result = 0;
                foreach (var j in json.AsArray)
                {
                    result |= Convert.ToInt32(itemSerializer.Deserialize(j));
                }
                return (T)Enum.ToObject(typeof(T), result);
            }

            public ImmutableJson Serialize(T value)
            {
                var type = value.GetType();
                var result = new JsonArray();
                var i = Convert.ToInt32(value);
                var n = 1;
                while (i > 0)
                {
                    if ((i & 1) == 1)
                    {
                        var val = (T)Enum.ToObject(type, n);
                        result.Insert(0, itemSerializer.Serialize(val));
                    }
                    i >>= 1;
                    n <<= 1;
                }
                return result;
            }
        }

#endregion

#region Default Serializers

        public static readonly ByteSerializer Byte = new ByteSerializer();
        public static readonly SByteSerializer SByte = new SByteSerializer();
        public static readonly ShortSerializer Short = new ShortSerializer();
        public static readonly UShortSerializer UShort = new UShortSerializer();
        public static readonly IntSerializer Int = new IntSerializer();
        public static readonly UIntSerializer UInt = new UIntSerializer();
        public static readonly LongSerializer Long = new LongSerializer();
        public static readonly ULongSerializer ULong = new ULongSerializer();
        public static readonly FloatSerializer Float = new FloatSerializer();
        public static readonly DoubleSerializer Double = new DoubleSerializer();
        public static readonly BoolSerializer Bool = new BoolSerializer();
        public static readonly StringSerializer String = new StringSerializer();
        public static readonly BinarySerializer Binary = new BinarySerializer();
        public static readonly JsonValueSerializer Json = new JsonValueSerializer();

        public static IJsonSerializer<List<T>> List<T>(IJsonSerializer<T> itemSerializer)
        {
            return new ListSerializer<T>(itemSerializer);
        }

        public static IJsonSerializer<IReadOnlyList<T>> ReadOnlyList<T>(IJsonSerializer<T> itemSerializer)
        {
            return new ReadOnlyListSerializer<T>(itemSerializer);
        }

        public static IJsonSerializer<Dictionary<TKey, TValue>> Dict<TKey, TValue>(IJsonKeySerializer<TKey> keySerializer, IJsonSerializer<TValue> valueSerializer) where TKey : notnull
        {
            return new DictSerializer<TKey, TValue>(keySerializer, valueSerializer);
        }

        public static IJsonSerializer<Dictionary<TKey, TValue>> Dict<TKey, TValue>(IJsonKeySerializer<TKey> keySerializer, IJsonSerializer<TValue> valueSerializer, IEqualityComparer<TKey> equalityComparer) where TKey : notnull
        {
            return new DictSerializer<TKey, TValue>(keySerializer, valueSerializer, equalityComparer);
        }

        public static IJsonSerializer<IReadOnlyDictionary<TKey, TValue>> ReadOnlyDict<TKey, TValue>(IJsonKeySerializer<TKey> keySerializer, IJsonSerializer<TValue> valueSerializer) where TKey : notnull
        {
            return new ReadOnlyDictSerializer<TKey, TValue>(keySerializer, valueSerializer);
        }

        public static IJsonSerializer<IReadOnlyDictionary<TKey, TValue>> ReadOnlyDict<TKey, TValue>(IJsonKeySerializer<TKey> keySerializer, IJsonSerializer<TValue> valueSerializer, IEqualityComparer<TKey> equalityComparer) where TKey : notnull
        {
            return new ReadOnlyDictSerializer<TKey, TValue>(keySerializer, valueSerializer, equalityComparer);
        }

        public static IJsonSerializer<T?> Nullable<T>(IJsonSerializer<T> valueSerializer) where T : struct
        {
            return new NullableSerializer<T>(valueSerializer);
        }

        public static IJsonSerializer<T?> Optional<T>(IJsonSerializer<T> valueSerializer) where T : class
        {
            return new OptionalSerializer<T>(valueSerializer);
        }

        public static IJsonSerializer<T> EnumFlags<T>(IJsonSerializer<T> itemSerializer) where T : Enum
        {
            return new EnumFlagsSerializer<T>(itemSerializer);
        }

#endregion
    }
}
