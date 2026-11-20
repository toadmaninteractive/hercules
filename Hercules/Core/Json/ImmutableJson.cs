using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Json
{
    public abstract class ImmutableJson : IEquatable<ImmutableJson>, IFormattable
    {
        #region Getters

        public abstract JsonType Type { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsNull => this is ImmutableJsonNull;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsBool => this is ImmutableJsonBoolean;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool AsBool => ((ImmutableJsonBoolean)this).Value;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsInt => this is ImmutableJsonInteger;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int AsInt => ((ImmutableJsonInteger)this).Value;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsLong => this is ImmutableJsonLong;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public long AsLong => IsInt ? AsInt : ((ImmutableJsonLong)this).Value;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsNumber => this is ImmutableJsonInteger || this is ImmutableJsonDouble;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public double AsNumber => IsInt ? AsInt : ((ImmutableJsonDouble)this).Value;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsString => this is ImmutableJsonString;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public string AsString => ((ImmutableJsonString)this).Value;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsArray => this is ImmutableJsonArray;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public ImmutableJsonArray AsArray => (ImmutableJsonArray)this;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsObject => this is ImmutableJsonObject;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public ImmutableJsonObject AsObject => (ImmutableJsonObject)this;

        public virtual ImmutableJson this[int index]
        {
            get { throw new InvalidCastException(string.Format("Unable to cast object of type '{0}' to type '{1}'.", GetType(), typeof(ImmutableJsonArray))); }
        }

        public virtual ImmutableJson this[string key]
        {
            get { throw new InvalidCastException(string.Format("Unable to cast object of type '{0}' to type '{1}'.", GetType(), typeof(ImmutableJsonObject))); }
        }

        #endregion

        #region Equals

        public static bool Equals(ImmutableJson? left, ImmutableJson? right)
        {
            if (left is null)
                return right is null;
            return left.Equals(right);
        }

        public abstract bool Equals(ImmutableJson? other);

        public override bool Equals(object? obj)
        {
            return this.Equals(obj as ImmutableJson);
        }

        public override int GetHashCode()
        {
            return GetType().GetHashCode();
        }

        protected static bool CompareDoubles(double x, double y)
        {
            var epsilon = 1e-14;

            if (x == y)
                return true;

            double diff = Math.Max(Math.Abs(x), Math.Abs(y)) * epsilon;
            return Math.Abs(x - y) <= diff;
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return ToString(null, null);
        }

        public string ToString(string? format)
        {
            return ToString(format, null);
        }

        public string ToString(IFormatProvider? formatProvider)
        {
            return ToString(null, formatProvider);
        }

        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            return JsonFormat.GetInstance(format, formatProvider).Format(this);
        }

        #endregion

        #region Path

        public ImmutableJson Fetch(JsonPath path)
        {
            ArgumentNullException.ThrowIfNull(path);

            ImmutableJson? data = this;
            foreach (var node in path.Nodes)
            {
                data = node.Fetch(data);
                if (data == null)
                    throw new ArgumentException("Failed to follow path node " + node);
            }

            return data;
        }

        public bool TryFetch(JsonPath path, [MaybeNullWhen(returnValue: false)] out ImmutableJson result)
        {
            ArgumentNullException.ThrowIfNull(path);

            result = default!;

            ImmutableJson? data = this;
            foreach (var node in path.Nodes)
            {
                data = node.Fetch(data);
                if (data == null)
                    return false;
            }

            result = data;
            return true;
        }

        public ImmutableJson Update(JsonPath path, ImmutableJson update)
        {
            ArgumentNullException.ThrowIfNull(path);
            ArgumentNullException.ThrowIfNull(update);
            if (path.Head == null)
                throw new InvalidOperationException("Json path is empty");

            return path.InternalUpdate(this, 0, update);
        }

        public ImmutableJson ForceUpdate(JsonPath path, ImmutableJson update)
        {
            ArgumentNullException.ThrowIfNull(path);
            ArgumentNullException.ThrowIfNull(update);
            if (path.Head == null)
                throw new InvalidOperationException("Json path is empty");

            return path.InternalForceUpdate(this, 0, update);
        }

        public ImmutableJson Delete(JsonPath path)
        {
            ArgumentNullException.ThrowIfNull(path);
            if (path.Head == null)
                throw new InvalidOperationException("Json path is empty");

            return path.InternalDelete(this, 0);
        }

        #endregion

        #region Singleton values

        public static readonly ImmutableJson Null = new ImmutableJsonNull();

        public static readonly ImmutableJson True = new ImmutableJsonBoolean(true);

        public static readonly ImmutableJson False = new ImmutableJsonBoolean(false);

        public static readonly ImmutableJson EmptyObject = ImmutableJsonObject.Empty;

        public static readonly ImmutableJson EmptyArray = ImmutableJsonArray.Empty;

        #endregion

        #region Create

        public static ImmutableJson Create(int value) => new ImmutableJsonInteger(value);

        public static ImmutableJson Create(long value) => new ImmutableJsonLong(value);

        public static ImmutableJson Create(bool value) => value ? True : False;

        public static ImmutableJson Create(double value) => new ImmutableJsonDouble(value);

        public static ImmutableJson Create(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return new ImmutableJsonString(value);
        }

        public static ImmutableJson Create(JsonArray value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return value.ToImmutable();
        }

        public static ImmutableJson Create(JsonObject value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return value.ToImmutable();
        }

        #endregion

        #region Conversions

        public static implicit operator ImmutableJson(int value) => Create(value);

        public static implicit operator ImmutableJson(long value) => Create(value);

        public static implicit operator ImmutableJson(double value) => Create(value);

        public static implicit operator ImmutableJson(bool value) => Create(value);

        public static implicit operator ImmutableJson(string value) => Create(value);

        public static implicit operator ImmutableJson(JsonArray value) => Create(value);

        public static implicit operator ImmutableJson(JsonObject value) => Create(value);

        #endregion
    }

    public sealed class ImmutableJsonNull : ImmutableJson
    {
        public override JsonType Type => JsonType.Null;

        internal ImmutableJsonNull()
        {
        }

        public override string ToString() => "null";

        public override bool Equals(ImmutableJson? other)
        {
            if (other is null)
                return false;
            return other.IsNull;
        }
    }

    public sealed class ImmutableJsonInteger : ImmutableJson
    {
        public override JsonType Type => JsonType.Number;

        public int Value { get; }

        internal ImmutableJsonInteger(int value)
        {
            Value = value;
        }

        public override bool Equals(ImmutableJson? other)
        {
            if (other is null)
                return false;
            if (other.IsInt)
                return other.AsInt == Value;
            if (other.IsLong)
                return other.AsLong == Value;
            if (other is ImmutableJsonDouble)
                return CompareDoubles(Value, ((ImmutableJsonDouble)other).Value);
            return false;
        }

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class ImmutableJsonLong : ImmutableJson
    {
        public override JsonType Type => JsonType.Number;

        public long Value { get; }

        internal ImmutableJsonLong(long value)
        {
            Value = value;
        }

        public override bool Equals(ImmutableJson? other)
        {
            if (other is null)
                return false;
            if (other.IsLong)
                return other.AsLong == Value;
            if (other.IsInt)
                return other.AsInt == Value;
            if (other is ImmutableJsonDouble)
                return CompareDoubles(Value, ((ImmutableJsonDouble)other).Value);
            return false;
        }

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class ImmutableJsonDouble : ImmutableJson
    {
        public override JsonType Type => JsonType.Number;

        public double Value { get; }

        internal ImmutableJsonDouble(double value)
        {
            Value = value;
        }

        public override bool Equals(ImmutableJson? other)
        {
            if (other is null)
                return false;
            if (other.IsNumber)
                return CompareDoubles(Value, other.AsNumber);
            return false;
        }

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class ImmutableJsonBoolean : ImmutableJson
    {
        public override JsonType Type => JsonType.Boolean;

        public bool Value { get; }

        internal ImmutableJsonBoolean(bool value)
        {
            Value = value;
        }

        public override bool Equals(ImmutableJson? other)
        {
            if (other is null)
                return false;
            if (other.IsBool)
                return other.AsBool == Value;
            return false;
        }

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => Value ? "true" : "false";
    }

    public sealed class ImmutableJsonString : ImmutableJson
    {
        public override JsonType Type => JsonType.String;

        public string Value { get; }

        internal ImmutableJsonString(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            Value = value;
        }

        public override bool Equals(ImmutableJson? other)
        {
            if (other is null)
                return false;
            if (other.IsString)
                return other.AsString == Value;
            return false;
        }

        public override int GetHashCode() => Value.GetHashCode();
    }

    public sealed class ImmutableJsonArray : ImmutableJson, IReadOnlyList<ImmutableJson>
    {
        public static readonly ImmutableJsonArray Empty = new ImmutableJsonArray(new List<ImmutableJson>(0));

        public override JsonType Type => JsonType.Array;

        readonly List<ImmutableJson> data;

        internal ImmutableJsonArray(List<ImmutableJson> source)
        {
            ArgumentNullException.ThrowIfNull(source);
            data = source;
            data.Capacity = data.Count;
        }

        public override bool Equals(ImmutableJson? other)
        {
            if (other is null)
                return false;
            if (other.IsArray)
                return CompareArrays(data, other.AsArray.data);
            return false;
        }

        public ImmutableJson ToMutable()
        {
            return new JsonArray(data, true);
        }

        #region IReadOnlyList

        public int Count => data.Count;

        public override ImmutableJson this[int index] => data[index];

        public IEnumerator<ImmutableJson> GetEnumerator() => data.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => data.GetEnumerator();

        #endregion

        public bool Contains(ImmutableJson item) => data.Contains(item);

        public int FindIndex(Predicate<ImmutableJson> match)
        {
            return data.FindIndex(match);
        }

        public int FindIndex(int startIndex, Predicate<ImmutableJson> match)
        {
            return data.FindIndex(startIndex, match);
        }

        public int FindIndex(int startIndex, int count, Predicate<ImmutableJson> match)
        {
            return data.FindIndex(startIndex, count, match);
        }

        public static implicit operator ImmutableJsonArray(JsonArray arr)
        {
            ArgumentNullException.ThrowIfNull(arr);
            return arr.ToImmutable();
        }

        static bool CompareArrays(List<ImmutableJson> first, List<ImmutableJson> second)
        {
            if (first == second)
                return true;

            if (first.Count != second.Count)
                return false;

            for (int i = 0; i < first.Count; i++)
            {
                if (!first[i].Equals(second[i]))
                    return false;
            }
            return true;
        }
    }

    public sealed class ImmutableJsonObject : ImmutableJson, IReadOnlyDictionary<string, ImmutableJson>
    {
        public static readonly ImmutableJsonObject Empty = new ImmutableJsonObject(new Dictionary<string, ImmutableJson>(0));

        public override JsonType Type => JsonType.Object;

        readonly Dictionary<string, ImmutableJson> data;

        internal ImmutableJsonObject(Dictionary<string, ImmutableJson> source)
        {
            ArgumentNullException.ThrowIfNull(source);
            data = source;
        }

        public override bool Equals(ImmutableJson? other)
        {
            if (other is null)
                return false;
            if (other.IsObject)
                return CompareObjects(data, other.AsObject.data);
            return false;
        }

        public JsonObject ToMutable()
        {
            return new JsonObject(data, true);
        }

        public ImmutableJson GetValueOrNull(string key)
        {
            if (data.TryGetValue(key, out var res))
                return res;
            return Null;
        }

#region  IReadOnlyDictionary

        public IEnumerable<string> Keys => data.Keys;

        public IEnumerable<ImmutableJson> Values => data.Values;

        public int Count => data.Count;

        public override ImmutableJson this[string key] => data[key];

        public bool ContainsKey(string key) => data.ContainsKey(key);

        public bool TryGetValue(string key, [MaybeNullWhen(returnValue: false)] out ImmutableJson value) => data.TryGetValue(key, out value);

        public IEnumerator<KeyValuePair<string, ImmutableJson>> GetEnumerator() => data.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => data.GetEnumerator();

#endregion

        public static implicit operator ImmutableJsonObject(JsonObject obj)
        {
            ArgumentNullException.ThrowIfNull(obj);
            return obj.ToImmutable();
        }

        static bool CompareObjects(Dictionary<string, ImmutableJson> first, Dictionary<string, ImmutableJson> second)
        {
            if (first == second)
                return true;

            if (first.Count != second.Count)
                return false;

            foreach (var kvp in first)
            {
                if (!second.TryGetValue(kvp.Key, out var secondValue))
                    return false;
                if (!kvp.Value.Equals(secondValue))
                    return false;
            }
            return true;
        }
    }
}
