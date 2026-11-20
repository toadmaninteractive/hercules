using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Json
{
    public sealed class JsonObject : IDictionary<string, ImmutableJson>
    {
        private Dictionary<string, ImmutableJson> data;
        private bool isSealed = false;

        public JsonObject()
        {
            data = new Dictionary<string, ImmutableJson>();
        }

        public JsonObject(int capacity)
        {
            data = new Dictionary<string, ImmutableJson>(capacity);
        }

        public JsonObject(IDictionary<string, ImmutableJson> dictionary)
        {
            ArgumentNullException.ThrowIfNull(dictionary);
            data = new Dictionary<string, ImmutableJson>(dictionary);
        }

        public JsonObject(IReadOnlyDictionary<string, ImmutableJson> dictionary)
        {
            ArgumentNullException.ThrowIfNull(dictionary);
            data = dictionary.ToDictionary(p => p.Key, p => p.Value);
        }

        internal JsonObject(Dictionary<string, ImmutableJson> dictionary, bool seal)
        {
            ArgumentNullException.ThrowIfNull(dictionary);
            data = dictionary;
            isSealed = seal;
        }

        public ImmutableJsonObject ToImmutable()
        {
            return data.Count == 0 ? ImmutableJsonObject.Empty : new ImmutableJsonObject(GetData(true));
        }

        public ImmutableJson GetValueOrNull(string key)
        {
            if (data.TryGetValue(key, out var res))
                return res;
            return ImmutableJson.Null;
        }

        #region ToString

        public override string ToString() => ToString(null, null);

        public string ToString(string? format) => ToString(format, null);

        public string ToString(IFormatProvider? formatProvider) => ToString(null, formatProvider);

        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            return JsonFormat.GetInstance(format, formatProvider).Format(this);
        }

        #endregion

        #region  IDictionary

        public bool IsReadOnly => false;

        public ICollection<string> Keys => data.Keys;

        public ICollection<ImmutableJson> Values => data.Values;

        public int Count => data.Count;

        public ImmutableJson this[string key]
        {
            get
            {
                ArgumentNullException.ThrowIfNull(key);
                return data[key];
            }
            set
            {
                ArgumentNullException.ThrowIfNull(key);
                ArgumentNullException.ThrowIfNull(value);
                Unseal();
                data[key] = value;
            }
        }

        public bool ContainsKey(string key)
        {
            return data.ContainsKey(key);
        }

        public void Add(string key, ImmutableJson value)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(value);
            Unseal();
            data.Add(key, value);
        }

        public bool Remove(string key)
        {
            ArgumentNullException.ThrowIfNull(key);
            Unseal();
            return data.Remove(key);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(returnValue: false)] out ImmutableJson value)
        {
            ArgumentNullException.ThrowIfNull(key);
            return data.TryGetValue(key, out value);
        }

        void ICollection<KeyValuePair<string, ImmutableJson>>.Add(KeyValuePair<string, ImmutableJson> item)
        {
            Unseal();
            ((ICollection<KeyValuePair<string, ImmutableJson>>)data).Add(item);
        }

        public void Clear()
        {
            if (isSealed)
            {
                data = new Dictionary<string, ImmutableJson>();
                isSealed = false;
            }
            else
                data.Clear();
        }

        bool ICollection<KeyValuePair<string, ImmutableJson>>.Contains(KeyValuePair<string, ImmutableJson> item)
        {
            return ((ICollection<KeyValuePair<string, ImmutableJson>>)data).Contains(item);
        }

        void ICollection<KeyValuePair<string, ImmutableJson>>.CopyTo(KeyValuePair<string, ImmutableJson>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, ImmutableJson>>)data).CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<string, ImmutableJson>>.Remove(KeyValuePair<string, ImmutableJson> item)
        {
            Unseal();
            return ((ICollection<KeyValuePair<string, ImmutableJson>>)data).Remove(item);
        }

        public IEnumerator<KeyValuePair<string, ImmutableJson>> GetEnumerator()
        {
            return data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return data.GetEnumerator();
        }

        #endregion

        internal Dictionary<string, ImmutableJson> GetData(bool seal)
        {
            isSealed |= seal;
            return data;
        }

        private void Unseal()
        {
            if (isSealed)
            {
                data = new Dictionary<string, ImmutableJson>(data);
                isSealed = false;
            }
        }
    }
}
