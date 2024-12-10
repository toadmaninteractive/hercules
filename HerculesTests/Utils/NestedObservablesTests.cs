using Moq;
using NUnit.Framework;
using System;

namespace Hercules.Utils.Tests
{
    [TestFixture]
    public class NestedObservablesTests
    {
#pragma warning disable CA1034 // Nested types should not be visible
        public record A(ObservableValue<B?> B);

        public class B
        {
            public ObservableValue<string?>? C { get; set; }
        }

        public interface ICallback<T>
        {
            void Callback(T value);
        }
#pragma warning restore CA1034 // Nested types should not be visible

        [Test]
        public void OnPropertyChangedTest()
        {
            var a = new A(new ObservableValue<B?>(null));
            var mock = new Mock<ICallback<string?>>();
            a.B.Switch(b => b?.C).Subscribe(mock.Object.Callback);
            a.B.Value = new B { C = new ObservableValue<string?>("5") };
            mock.Verify(m => m.Callback("5"));
            a.B.Value.C.Value = "10";
            mock.Verify(m => m.Callback("10"));
            a.B.Value = new B { C = new ObservableValue<string?>("2") };
            mock.Verify(m => m.Callback("2"));
            a.B.Value.C.Value = "3";
            mock.Verify(m => m.Callback("3"));
        }
    }
}
