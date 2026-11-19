using System;
using System.Collections;
using System.Collections.Generic;

namespace Json
{
    public sealed class JsonArray : IList<ImmutableJson>
    {
        private List<ImmutableJson> data;
        private bool isSealed = false;

        public JsonArray()
        {
            data = new List<ImmutableJson>();
        }

        public JsonArray(int capacity)
        {
            data = new List<ImmutableJson>(capacity);
        }

        public JsonArray(IEnumerable<ImmutableJson> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            data = new List<ImmutableJson>(collection);
        }

        internal JsonArray(List<ImmutableJson> list, bool seal)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            data = list;
            isSealed = seal;
        }

        public ImmutableJsonArray ToImmutable()
        {
            return data.Count == 0 ? ImmutableJsonArray.Empty : new ImmutableJsonArray(GetData(true));
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

        #region IList

        public bool IsReadOnly => false;

        public int Count => data.Count;

        public ImmutableJson this[int index]
        {
            get
            {
                return data[index];
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                Unseal();
                data[index] = value;
            }
        }

        public int IndexOf(ImmutableJson item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            return data.IndexOf(item);
        }

        public void Insert(int index, ImmutableJson item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            Unseal();
            data.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            Unseal();
            data.RemoveAt(index);
        }

        public void Add(ImmutableJson item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            Unseal();
            data.Add(item);
        }

        public void Clear()
        {
            if (isSealed)
            {
                data = new List<ImmutableJson>();
                isSealed = false;
            }
            else
                data.Clear();
        }

        public bool Contains(ImmutableJson item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            return data.Contains(item);
        }

        public void CopyTo(ImmutableJson[] array, int arrayIndex)
        {
            data.CopyTo(array, arrayIndex);
        }

        public bool Remove(ImmutableJson item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            Unseal();
            return data.Remove(item);
        }

        public IEnumerator<ImmutableJson> GetEnumerator()
        {
            return data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return data.GetEnumerator();
        }

        #endregion

        #region List

        public int Capacity
        {
            get { return data.Capacity; }
            set { data.Capacity = value; }
        }

        public void AddRange(IEnumerable<ImmutableJson> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            Unseal();
            data.AddRange(collection);
        }

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

        public ImmutableJson? Find(Predicate<ImmutableJson> match)
        {
            return data.Find(match);
        }

        public void RemoveRange(int index, int count)
        {
            Unseal();
            data.RemoveRange(index, count);
        }

        public int RemoveAll(Predicate<ImmutableJson> match)
        {
            Unseal();
            return data.RemoveAll(match);
        }

        public void Reverse()
        {
            Unseal();
            data.Reverse();
        }

        public void Reverse(int index, int count)
        {
            Unseal();
            data.Reverse(index, count);
        }

        public void Sort(Comparison<ImmutableJson> comparison)
        {
            Unseal();
            data.Sort(comparison);
        }

        public void Sort(IComparer<ImmutableJson> comparer)
        {
            Unseal();
            data.Sort(comparer);
        }

        public void Sort(int index, int count, IComparer<ImmutableJson> comparer)
        {
            Unseal();
            data.Sort(index, count, comparer);
        }

        #endregion

        internal List<ImmutableJson> GetData(bool seal)
        {
            isSealed |= seal;
            return data;
        }

        private void Unseal()
        {
            if (isSealed)
            {
                data = new List<ImmutableJson>(data);
                isSealed = false;
            }
        }
    }
}
