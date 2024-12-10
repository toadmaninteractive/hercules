using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Hercules
{
    public static class Optional
    {
        public static Optional<T> Some<T>(T value) => new(value);
        public static ref readonly Optional<T> None<T>() => ref Optional<T>.None;

        public static Optional<T> ToOptional<T>(this T? nullableValue) where T : struct
        {
            if (nullableValue.HasValue)
                return Some(nullableValue.Value);
            else
                return None<T>();
        }

        public static Optional<T?> ToOptional<T>(this T? nullableReference) where T : class
        {
            if (nullableReference == null)
                return None<T?>();
            else
                return Some<T?>(nullableReference);
        }
    }

    public readonly struct Optional<T> : IEquatable<Optional<T>>
    {
        public T Value { get; }
        public bool HasValue { get; }

        private Optional(T value, bool hasValue)
        {
            Value = value;
            HasValue = hasValue;
        }

        public Optional(T value)
        {
            Value = value;
            HasValue = true;
        }

        [return: MaybeNull]
        public T GetValueOrDefault() => HasValue ? Value : default;

        public T GetValueOrDefault(T @default) => HasValue ? Value : @default;

        public static readonly Optional<T> None = new Optional<T>(default!, false);

        public override int GetHashCode()
        {
            return HashCode.Combine(HasValue, Value);
        }

        public override bool Equals(object? obj)
        {
            if (obj is Optional<T> optional)
                return Equals(optional);
            else
                return false;
        }

        public bool Equals(Optional<T> other)
        {
            if (HasValue != other.HasValue)
                return false;
            return !HasValue || EqualityComparer<T>.Default.Equals(Value, other.Value);
        }

        public static bool operator ==(Optional<T> left, Optional<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Optional<T> left, Optional<T> right)
        {
            return !left.Equals(right);
        }
    }
}
