using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Hercules
{
    internal sealed class ReadOnlyObservableValueCollection<T> : NotifyPropertyChanged, IReadOnlyObservableValue<IReadOnlyList<T>>, IDisposable, IReadOnlyList<T>
    {
        private readonly IReadOnlyList<IReadOnlyObservableValue<T>> sources;
        private readonly IDisposable subscriptionToSources;

        public IReadOnlyList<T> Value => this;

        public int Count => sources.Count;

        public T this[int index] => sources[index].Value;

        public IDisposable Subscribe(IObserver<IReadOnlyList<T>> observer)
        {
            return Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(h => PropertyChanged += h, h => PropertyChanged -= h).Select(_ => Value).Subscribe(observer);
        }

        public ReadOnlyObservableValueCollection(IReadOnlyList<IReadOnlyObservableValue<T>> sources)
        {
            this.sources = sources;
            this.subscriptionToSources = new CompositeDisposable(sources.Select(s => s.Subscribe(SourceChanged)));
        }

        private void SourceChanged(T value)
        {
            RaisePropertyChanged(nameof(Value));
        }

        public void Dispose()
        {
            subscriptionToSources.Dispose();
        }

        public IEnumerator<T> GetEnumerator() => sources.Select(s => s.Value).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => sources.Select(s => s.Value).GetEnumerator();
    }

    public static class ObservableValues
    {
        public static IReadOnlyObservableValue<bool> Any(params IReadOnlyObservableValue<bool>[] sources)
        {
            return new ReadOnlyObservableValueCollection<bool>(sources).Wrap(values => values.Any(v => v));
        }
    }
}
