using System;
using System.ComponentModel;
using System.Reactive.Linq;

namespace Hercules
{
    public interface ISetting : INotifyPropertyChanged
    {
        string Name { get; }

        void Write(ISettingsWriter writer);

        void Read(ISettingsReader reader);
    }

    public class Setting<T> : NotifyPropertyChanged, ISetting, IReadOnlyObservableValue<T>, IObservableValue<T>, IDisposable
    {
        public string Name { get; }

        private T value;

        public T Value
        {
            get => value;
            set => SetField(ref this.value, value);
        }

        public Setting(string name, T value)
        {
            this.Name = name;
            this.value = value;
        }

        public override string? ToString()
        {
            return value == null ? "null" : value.ToString();
        }

        public virtual void Write(ISettingsWriter writer)
        {
            writer.Write(Name, Value);
        }

        public virtual void Read(ISettingsReader reader)
        {
            if (reader.Read<T>(Name, out var val))
                Value = val;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(h => PropertyChanged += h, h => PropertyChanged -= h).Select(_ => value).Subscribe(observer);
        }

        public void Dispose()
        {
        }
    }

    public class TimeZoneSetting : Setting<TimeZoneInfo>
    {
        public TimeZoneSetting(string name, TimeZoneInfo value) : base(name, value)
        {
        }

        public override void Write(ISettingsWriter writer)
        {
            writer.Write(Name, Value.Id);
        }

        public override void Read(ISettingsReader reader)
        {
            if (reader.Read<string>(Name, out var val))
                Value = TimeZoneInfo.FindSystemTimeZoneById(val);
        }
    }
}
