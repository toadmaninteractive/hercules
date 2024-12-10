using Igor.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Unreal
{
    public class FStringBinarySerializer : IBinarySerializer<string>
    {
        public string Deserialize(BinaryReader reader)
        {
            // > 0 for ANSICHAR, < 0 for UCS2CHAR serialization
            var saveNum = reader.ReadInt32();
            bool loadUCS2Char = saveNum < 0;
            if (loadUCS2Char)
            {
                // If SaveNum cannot be negated due to integer overflow, Ar is corrupted.
                if (saveNum == int.MinValue)
                {
                    throw new FileLoadException("Archive is corrupted");
                }

                saveNum = -saveNum;
            }

            if (saveNum == 0) return string.Empty;

            // 1 byte is removed because of null terminator (\0)
            if (loadUCS2Char)
            {
                char[] data = new char[saveNum];
                for (int i = 0; i < saveNum; i++)
                {
                    data[i] = (char)reader.ReadUInt16();
                }
                return new string(data, 0, data.Length - 1);
            }
            else
            {
                return Encoding.UTF8.GetString(reader.ReadBytes(saveNum).AsSpan(..^1));
            }
        }

        public void Serialize(BinaryWriter writer, string value)
        {
            // byte[] data = Encoding.UTF8.GetBytes(value);
            // WriteSize(writer, data.Length);
            // writer.Write(data);
        }
    }

    public struct TArrayBinarySerializer<T> : IBinarySerializer<IReadOnlyList<T>>
    {
        readonly IBinarySerializer<T> itemSerializer;

        public IReadOnlyList<T> Deserialize(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            List<T> result = new List<T>(count);
            for (int i = 0; i < count; i++)
                result.Add(itemSerializer.Deserialize(reader));
            return result;
        }

        public void Serialize(BinaryWriter writer, IReadOnlyList<T> value)
        {
            writer.Write(value.Count);
            foreach (var item in value)
                itemSerializer.Serialize(writer, item);
        }

        public TArrayBinarySerializer(IBinarySerializer<T> itemSerializer)
        {
            this.itemSerializer = itemSerializer;
        }
    }

    public class BinaryBinarySerializer : IBinarySerializer<byte[]>
    {
        public byte[] Deserialize(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            return reader.ReadBytes(count);
        }

        public void Serialize(BinaryWriter writer, byte[] value)
        {
            writer.Write(value.Length);
            writer.Write(value);
        }
    }

    public static class UnrealTypes
    {
        public static TArrayBinarySerializer<T> TArray<T>(IBinarySerializer<T> itemSerializer)
        {
            return new TArrayBinarySerializer<T>(itemSerializer);
        }

        public static readonly FStringBinarySerializer FString = new FStringBinarySerializer();

        public static readonly BinaryBinarySerializer Binary = new BinaryBinarySerializer();
    }
}
