using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Windows.Media.TextFormatting;

namespace Hercules
{
    public interface IReadOnlyObservableValue<T> : IObservable<T>, INotifyPropertyChanged, IDisposable
    {
        T Value { get; }
    }

    public interface IObservableValue<T> : IObservable<T>, INotifyPropertyChanged, IDisposable
    {
        T Value { get; set; }
    }

    public sealed class ObservableValue<T> : NotifyPropertyChanged, IObservableValue<T>, IReadOnlyObservableValue<T>, IDisposable
    {
        private T value;

        public T Value
        {
            get => value;
            set => SetField(ref this.value, value);
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(h => PropertyChanged += h, h => PropertyChanged -= h).Select(_ => value).Subscribe(observer);
        }

        public ObservableValue(T value)
        {
            this.value = value;
        }

        public override string? ToString()
        {
            return value?.ToString();
        }

        public void Dispose()
        {
        }
    }

    public sealed class ObservableValueWrapper<TSource, TValue> : NotifyPropertyChanged, IReadOnlyObservableValue<TValue>, IDisposable where TSource : INotifyPropertyChanged
    {
        private readonly TSource source;
        private readonly Func<TSource, TValue> wrapper;
        private readonly string propertyName;
        private TValue value;

        public TValue Value => value;

        public IDisposable Subscribe(IObserver<TValue> observer)
        {
            return Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(h => PropertyChanged += h, h => PropertyChanged -= h).Select(_ => value).Subscribe(observer);
        }

        public ObservableValueWrapper(TSource source, Func<TSource, TValue> wrapper, string propertyName)
        {
            this.source = source;
            this.wrapper = wrapper;
            this.propertyName = propertyName;
            this.value = wrapper(source);
            source.PropertyChanged += Source_PropertyChanged;
        }

        private void Source_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == propertyName)
            {
                var newValue = wrapper(source);
                if (!EqualityComparer<TValue>.Default.Equals(value, newValue))
                {
                    value = newValue;
                    RaisePropertyChanged(nameof(Value));
                }
            }
        }

        public override string? ToString()
        {
            return value?.ToString();
        }

        public void Dispose()
        {
            source.PropertyChanged -= Source_PropertyChanged;
        }
    }
}
