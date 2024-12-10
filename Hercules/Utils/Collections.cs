using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;

namespace Hercules
{
    public static class EnumerableHelper
    {
        public static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> o) where T : class
            => o.Where(x => x != null)!;

        public static IEnumerable<IReadOnlyList<T>> GroupAdjacentBy<T>(this IEnumerable<T> source, Func<T, T, bool> predicate)
        {
            using var e = source.GetEnumerator();
            if (e.MoveNext())
            {
                var list = new List<T> { e.Current };
                var prev = e.Current;
                while (e.MoveNext())
                {
                    if (predicate(prev, e.Current))
                    {
                        list.Add(e.Current);
                    }
                    else
                    {
                        yield return list;
                        list = new List<T> { e.Current };
                    }
                    prev = e.Current;
                }
                yield return list;
            }
        }

        public static IEnumerable EnumerateCompositeCollection(IEnumerable collection)
        {
            if (collection is CompositeCollection compositeCollection)
            {
                foreach (var collectionContainer in compositeCollection.Cast<CollectionContainer>())
                {
                    foreach (var item in EnumerateCompositeCollection(collectionContainer.Collection))
                        yield return item;
                }
            }
            else
            {
                foreach (var item in collection)
                    yield return item;
            }
        }

        public static IEnumerable<TGroup> ZipByKey<T, TGroup>(this IReadOnlyCollection<T> first, IReadOnlyCollection<T> second, Func<T, string> keyFun, Func<T?, T?, TGroup> groupFun)
        {
            var keys = first.Select(keyFun).Concat(second.Select(keyFun)).Distinct().ToList();
            return
                from key in keys
                select groupFun(first.FirstOrDefault(i => keyFun(i) == key), second.FirstOrDefault(i => keyFun(i) == key));
        }

        public static IEnumerable<(T?, T?)> ZipByKey<T>(this IReadOnlyCollection<T> first, IReadOnlyCollection<T> second, Func<T, string> keyFun)
        {
            var keys = first.Select(keyFun).Concat(second.Select(keyFun)).Distinct().ToList();
            return
                from key in keys
                select (first.FirstOrDefault(i => keyFun(i) == key), second.FirstOrDefault(i => keyFun(i) == key));
        }
    }

    public static class ObservableCollectionHelper
    {
        public static void InsertSorted<T>(this ObservableCollection<T> collection, T item) where T : IComparable<T>
        {
            if (collection.Count == 0)
                collection.Add(item);
            else
            {
                bool last = true;
                for (int i = 0; i < collection.Count; i++)
                {
                    int result = collection[i].CompareTo(item);
                    if (result >= 1)
                    {
                        collection.Insert(i, item);
                        last = false;
                        break;
                    }
                }
                if (last)
                    collection.Add(item);
            }
        }

        public static void SynchronizeSorted<T>(this ObservableCollection<T> collection, IEnumerable<T> source) where T : IComparable<T>
        {
            int index = 0;
            foreach (var sourceItem in source)
            {
                while (index < collection.Count && collection[index].CompareTo(sourceItem) < 0)
                    collection.RemoveAt(index);
                if (index >= collection.Count || collection[index].CompareTo(sourceItem) > 0)
                    collection.Insert(index, sourceItem);
                index++;
            }
            if (index == 0)
            {
                collection.Clear();
            }
            else
            {
                while (index < collection.Count)
                    collection.RemoveAt(index);
            }
        }

        public static void SynchronizeOrdered<T>(this ObservableCollection<T> collection, IEnumerable<T> source) where T : IEquatable<T>
        {
            int? FindIndex(int fromIndex, T sourceItem)
            {
                for (int j = fromIndex; j < collection.Count; j++)
                {
                    if (collection[j].Equals(sourceItem))
                    {
                        return j;
                    }
                }

                return null;
            }

            int index = 0;
            foreach (var sourceItem in source)
            {
                var sourceIndex = FindIndex(index, sourceItem);
                if (sourceIndex.HasValue)
                {
                    if (sourceIndex.Value != index)
                        collection.Move(sourceIndex.Value, index);
                }
                else
                {
                    collection.Insert(index, sourceItem);
                }
                index++;
            }

            if (index == 0)
            {
                collection.Clear();
            }
            else
            {
                while (index < collection.Count)
                    collection.RemoveAt(index);
            }
        }

        public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
        {
            foreach (var item in items)
                collection.Add(item);
        }

        public static void RemoveRange<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
        {
            foreach (var item in items)
                collection.Remove(item);
        }

        public static void RemoveAll<T>(this ObservableCollection<T> collection, Predicate<T> predicate)
        {
            var toRemove = collection.Where(n => predicate(n)).ToList();
            foreach (var n in toRemove)
                collection.Remove(n);
        }

        public static void MoveUp<T>(this ObservableCollection<T> collection, T item)
        {
            var index = collection.IndexOf(item);
            collection.Move(index, index - 1);
        }

        public static void MoveDown<T>(this ObservableCollection<T> collection, T item)
        {
            var index = collection.IndexOf(item);
            collection.Move(index, index + 1);
        }
    }

    public static class DictionaryHelper
    {
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TValue> creator) where TKey : notnull
        {
            if (!dict.TryGetValue(key, out var value))
            {
                value = creator();
                dict.Add(key, value);
            }

            return value;
        }
    }
}
