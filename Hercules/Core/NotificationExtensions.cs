using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;

namespace Hercules
{
    public static class NotificationExtensions
    {
        public static IObservable<NotifyCollectionChangedEventArgs> OnCollectionChanged<T>(this ObservableCollection<T> collection)
            where T : INotifyPropertyChanged
        {
            return Observable.Create<NotifyCollectionChangedEventArgs>(o =>
            {
                return Observable
                    .FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(handler => handler.Invoke, h => collection.CollectionChanged += h, h => collection.CollectionChanged -= h)
                    .Select(e => e.EventArgs)
                    .Subscribe(o);
            });
        }

        public static IObservable<Unit> OnPropertyChanged<T>(this T source, string propertyName)
            where T : notnull, INotifyPropertyChanged
        {
            return Observable.Create<Unit>(o =>
            {
                return Observable
                    .FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(handler => handler.Invoke, h => source.PropertyChanged += h, h => source.PropertyChanged -= h)
                    .Where(e => e.EventArgs.PropertyName == propertyName)
                    .Select(_ => Unit.Default)
                    .Subscribe(o);
            });
        }

        public static IObservable<TProperty> OnPropertyChanged<T, TProperty>(this T source, string propertyName, Func<T, TProperty> selector)
            where T : notnull, INotifyPropertyChanged
        {
            return Observable.Create<TProperty>(o =>
            {
                return Observable
                    .FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(handler => handler.Invoke, h => source.PropertyChanged += h, h => source.PropertyChanged -= h)
                    .Where(e => e.EventArgs.PropertyName == propertyName)
                    .Select(e => selector(source))
                    .Subscribe(o);
            });
        }

        public static IObservable<T2?> Switch<T1, T2>(this IReadOnlyObservableValue<T1> source, Func<T1, IReadOnlyObservableValue<T2>?> func)
        {
            static IObservable<T?> CurrentAndNextValues<T>(IReadOnlyObservableValue<T>? source)
            {
                return source == null ? Observable.Return<T?>(default) : Observable.Return(source.Value).Concat(source);
            }

            return source.Select(s => s == null ? Observable.Return<T2?>(default) : CurrentAndNextValues(func(s))).Switch();
        }

        public static IReadOnlyObservableValue<TValue> Wrap<TSource, TValue>(this IReadOnlyObservableValue<TSource> source, Func<TSource, TValue> wrapper)
        {
            return new ObservableValueWrapper<IReadOnlyObservableValue<TSource>, TValue>(source, s => wrapper(s.Value), nameof(IReadOnlyObservableValue<TSource>.Value));
        }

        public static IReadOnlyObservableValue<TValue> ObserveProperty<TSource, TValue>(this TSource source, string propertyName, Func<TSource, TValue> getter) where TSource : INotifyPropertyChanged
        {
            return new ObservableValueWrapper<TSource, TValue>(source, getter, propertyName);
        }
    }
}