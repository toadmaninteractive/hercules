using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Igor.Serialization
{
    public interface IBinarySerializer<T>
    {
        T Deserialize(BinaryReader reader);

        void Serialize(BinaryWriter writer, T value);
    }

    public static class IgorSerializer
    {
        public class ByteSerializer : IBinarySerializer<byte>
        {
            public byte Deserialize(BinaryReader reader)
            {
                return reader.ReadByte();
            }

            public void Serialize(BinaryWriter writer, byte value)
            {
                writer.Write(value);
            }
        }

        public class SByteSerializer : IBinarySerializer<sbyte>
        {
            public sbyte Deserialize(BinaryReader reader)
            {
                return reader.ReadSByte();
            }

            public void Serialize(BinaryWriter writer, sbyte value)
            {
                writer.Write(value);
            }
        }

        public class ShortSerializer : IBinarySerializer<short>
        {
            public short Deserialize(BinaryReader reader)
            {
                return reader.ReadInt16();
            }

            public void Serialize(BinaryWriter writer, short value)
            {
                writer.Write(value);
            }
        }

        public class UShortSerializer : IBinarySerializer<ushort>
        {
            public ushort Deserialize(BinaryReader reader)
            {
                return reader.ReadUInt16();
            }

            public void Serialize(BinaryWriter writer, ushort value)
            {
                writer.Write(value);
            }
        }

        public class IntSerializer : IBinarySerializer<int>
        {
            public int Deserialize(BinaryReader reader)
            {
                return reader.ReadInt32();
            }

            public void Serialize(BinaryWriter writer, int value)
            {
                writer.Write(value);
            }
        }

        public class UIntSerializer : IBinarySerializer<uint>
        {
            public uint Deserialize(BinaryReader reader)
            {
                return reader.ReadUInt32();
            }

            public void Serialize(BinaryWriter writer, uint value)
            {
                writer.Write(value);
            }
        }

        public class LongSerializer : IBinarySerializer<long>
        {
            public long Deserialize(BinaryReader reader)
            {
                return reader.ReadInt64();
            }

            public void Serialize(BinaryWriter writer, long value)
            {
                writer.Write(value);
            }
        }

        public class ULongSerializer : IBinarySerializer<ulong>
        {
            public ulong Deserialize(BinaryReader reader)
            {
                return reader.ReadUInt64();
            }

            public void Serialize(BinaryWriter writer, ulong value)
            {
                writer.Write(value);
            }
        }

        public class FloatSerializer : IBinarySerializer<float>
        {
            [StructLayout(LayoutKind.Explicit)]
            private struct FloatToIntConverter
            {
                [FieldOffset(0)]
                public float FloatValue;

                [FieldOffset(0)]
                public uint IntValue;
            }

            public float Deserialize(BinaryReader reader)
            {
                return reader.ReadSingle();
            }

            public void Serialize(BinaryWriter writer, float value)
            {
                var convertor = new FloatToIntConverter { FloatValue = value };
                writer.Write(convertor.IntValue);
            }
        }

        public class DoubleSerializer : IBinarySerializer<double>
        {
            [StructLayout(LayoutKind.Explicit)]
            private struct DoubleToLongConverter
            {
                [FieldOffset(0)]
                public double DoubleValue;

                [FieldOffset(0)]
                public ulong LongValue;
            }

            public double Deserialize(BinaryReader reader)
            {
                return reader.ReadDouble();
            }

            public void Serialize(BinaryWriter writer, double value)
            {
                var convertor = new DoubleToLongConverter { DoubleValue = value };
                writer.Write(convertor.LongValue);
            }
        }

        public class BoolSerializer : IBinarySerializer<bool>
        {
            public bool Deserialize(BinaryReader reader)
            {
                return reader.ReadBoolean();
            }

            public void Serialize(BinaryWriter writer, bool value)
            {
                writer.Write(value);
            }
        }

        public class StringSerializer : IBinarySerializer<string>
        {
            public string Deserialize(BinaryReader reader)
            {
                int len = ReadSize(reader);
                if (len > 0)
                {
                    byte[] buf = new byte[len];
                    reader.Read(buf, 0, buf.Length);
                    return Encoding.UTF8.GetString(buf);
                }
                else
                {
                    return string.Empty;
                }
            }

            public void Serialize(BinaryWriter writer, string value)
            {
                byte[] data = Encoding.UTF8.GetBytes(value);
                WriteSize(writer, data.Length);
                writer.Write(data);
            }
        }

        public class BinarySerializer : IBinarySerializer<byte[]>
        {
            public byte[] Deserialize(BinaryReader reader)
            {
                return reader.ReadBytes(ReadSize(reader));
            }

            public void Serialize(BinaryWriter writer, byte[] value)
            {
                WriteSize(writer, value.Length);
                if (value.Length > 0)
                    writer.Write(value, 0, value.Length);
            }
        }

        public struct NullableSerializer<T> : IBinarySerializer<T?> where T : struct
        {
            IBinarySerializer<T> valueSerializer;

            public T? Deserialize(BinaryReader reader)
            {
                if (reader.ReadBoolean())
                    return valueSerializer.Deserialize(reader);
                else
                    return null;
            }

            public void Serialize(BinaryWriter writer, T? value)
            {
                writer.Write(value.HasValue);
                if (value.HasValue)
                    valueSerializer.Serialize(writer, value.Value);
            }

            public NullableSerializer(IBinarySerializer<T> valueSerializer)
            {
                this.valueSerializer = valueSerializer;
            }
        }

        public struct OptionalSerializer<T> : IBinarySerializer<T> where T : class
        {
            IBinarySerializer<T> valueSerializer;

            public T Deserialize(BinaryReader reader)
            {
                if (reader.ReadBoolean())
                    return valueSerializer.Deserialize(reader);
                else
                    return null;
            }

            public void Serialize(BinaryWriter writer, T value)
            {
                writer.Write(value != null);
                if (value != null)
                    valueSerializer.Serialize(writer, value);
            }

            public OptionalSerializer(IBinarySerializer<T> valueSerializer)
            {
                this.valueSerializer = valueSerializer;
            }
        }

        public struct ListSerializer<T> : IBinarySerializer<List<T>>
        {
            IBinarySerializer<T> itemSerializer;

            public List<T> Deserialize(BinaryReader reader)
            {
                int count = ReadSize(reader);
                List<T> result = new List<T>(count);
                for (int i = 0; i < count; i++)
                    result.Add(itemSerializer.Deserialize(reader));
                return result;
            }

            public void Serialize(BinaryWriter writer, List<T> value)
            {
                WriteSize(writer, value.Count);
                foreach (var item in value)
                    itemSerializer.Serialize(writer, item);
            }

            public ListSerializer(IBinarySerializer<T> itemSerializer)
            {
                this.itemSerializer = itemSerializer;
            }
        }

        public struct DictSerializer<TKey, TValue> : IBinarySerializer<Dictionary<TKey, TValue>>
        {
            IBinarySerializer<TKey> keySerializer;
            IBinarySerializer<TValue> valueSerializer;
            IEqualityComparer<TKey> equalityComparer;

            public Dictionary<TKey, TValue> Deserialize(BinaryReader reader)
            {
                int count = ReadSize(reader);
                var result = equalityComparer == null ? new Dictionary<TKey, TValue>() : new Dictionary<TKey, TValue>(equalityComparer);
                for (int i = 0; i < count; i++)
                {
                    TKey key = keySerializer.Deserialize(reader);
                    TValue value = valueSerializer.Deserialize(reader);
                    result.Add(key, value);
                }
                return result;
            }

            public void Serialize(BinaryWriter writer, Dictionary<TKey, TValue> value)
            {
                WriteSize(writer, value.Count);

                foreach (var pair in value)
                {
                    keySerializer.Serialize(writer, pair.Key);
                    valueSerializer.Serialize(writer, pair.Value);
                }
            }

            public DictSerializer(IBinarySerializer<TKey> keySerializer, IBinarySerializer<TValue> valueSerializer)
                : this(keySerializer, valueSerializer, null)
            {
            }

            public DictSerializer(IBinarySerializer<TKey> keySerializer, IBinarySerializer<TValue> valueSerializer, IEqualityComparer<TKey> equalityComparer)
            {
                this.keySerializer = keySerializer;
                this.valueSerializer = valueSerializer;
                this.equalityComparer = equalityComparer;
            }
        }

        public static int ReadSize(BinaryReader reader)
        {
            int len = (int)ReadVarint32(reader);
            if (len < 0)
                throw new InvalidOperationException("Decode negative length");
            return len;
        }

        static uint ReadVarint32(BinaryReader reader)
        {
            uint n = 0;
            int s = 0;
            while (true)
            {
                uint t = (uint)reader.ReadByte();
                if ((t & 0x80) == 0)
                {
                    n |= t << s;
                    break;
                }
                else
                {
                    n |= (t & 127) << s;
                    s += 7;
                }
            }
            return n;
        }

        public static void WriteSize(BinaryWriter writer, int length)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(length);
            WriteVarint32(writer, (uint)length);
        }

        static void WriteVarint32(BinaryWriter writer, uint value)
        {
            while (true)
            {
                if (value <= 127)
                {
                    writer.Write((byte)value);
                    break;
                }
                else
                {
                    writer.Write((byte)(0x80 | (value & 127)));
                    value >>= 7;
                }
            }
        }

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

        public static ListSerializer<T> List<T>(IBinarySerializer<T> itemSerializer)
        {
            return new ListSerializer<T>(itemSerializer);
        }

        public static DictSerializer<TKey, TValue> Dict<TKey, TValue>(IBinarySerializer<TKey> keySerializer, IBinarySerializer<TValue> valueSerializer)
        {
            return new DictSerializer<TKey, TValue>(keySerializer, valueSerializer);
        }

        public static DictSerializer<TKey, TValue> Dict<TKey, TValue>(IBinarySerializer<TKey> keySerializer, IBinarySerializer<TValue> valueSerializer, IEqualityComparer<TKey> equalityComparer)
        {
            return new DictSerializer<TKey, TValue>(keySerializer, valueSerializer, equalityComparer);
        }

        public static NullableSerializer<T> Nullable<T>(IBinarySerializer<T> valueSerializer) where T : struct
        {
            return new NullableSerializer<T>(valueSerializer);
        }

        public static OptionalSerializer<T> Optional<T>(IBinarySerializer<T> valueSerializer) where T : class
        {
            return new OptionalSerializer<T>(valueSerializer);
        }
    }
}
