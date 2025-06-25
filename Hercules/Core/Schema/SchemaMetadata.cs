using Json;
using System.Diagnostics.CodeAnalysis;

namespace Igor.Schema
{
    public static class SchemaMetadata
    {
        public static bool GetBoolMetadata(this Descriptor descriptor, string name) =>
            descriptor.Meta != null && descriptor.Meta.TryGetValue(name, out var json) && json != null && json.Equals(ImmutableJson.True);

        public static bool GetBoolMetadata(this Descriptor descriptor, string name, bool @default)
        {
            if (descriptor.Meta != null && descriptor.Meta.TryGetValue(name, out var json) && json != null && json.IsBool)
                return json.AsBool;
            else
                return @default;
        }

        public static bool GetBoolMetadata(this CustomType customType, string name) =>
            customType.Meta != null && customType.Meta.TryGetValue(name, out var json) && json != null && json.Equals(ImmutableJson.True);

        public static bool TryGetStringMetadata(this CustomType customType, string name, [MaybeNullWhen(returnValue: false)] out string value)
        {
            if (customType.Meta != null && customType.Meta.TryGetValue(name, out var json) && json != null && json.IsString)
            {
                value = json.AsString;
                return true;
            }
            value = null;
            return false;
        }

        public static string? GetStringMetadata(this CustomType customType, string name) =>
            customType.Meta != null && customType.Meta.TryGetValue(name, out var json) && json != null && json.IsString ? json.AsString : null;

        public static string GetStringMetadata(this CustomType customType, string name, string @default) =>
            customType.Meta != null && customType.Meta.TryGetValue(name, out var json) && json != null && json.IsString ? json.AsString : @default;

        public static string? GetStringMetadata(this Descriptor descriptor, string name) =>
            descriptor.Meta != null && descriptor.Meta.TryGetValue(name, out var json) && json != null && json.IsString ? json.AsString : null;

        public static string GetStringMetadata(this Descriptor descriptor, string name, string @default) =>
            descriptor.Meta != null && descriptor.Meta.TryGetValue(name, out var json) && json != null && json.IsString ? json.AsString : @default;

        public static double? GetFloatMetadata(this Descriptor descriptor, string name) =>
            descriptor.Meta != null && descriptor.Meta.TryGetValue(name, out var json) && json != null && json.IsNumber ? json.AsNumber : null;

        public static double GetFloatMetadata(this Descriptor descriptor, string name, double @default) =>
            descriptor.Meta != null && descriptor.Meta.TryGetValue(name, out var json) && json != null && json.IsNumber ? json.AsNumber : @default;

        public static int? GetIntMetadata(this Descriptor descriptor, string name) =>
            descriptor.Meta != null && descriptor.Meta.TryGetValue(name, out var json) && json != null && json.IsInt ? json.AsInt : null;

        public static int GetIntMetadata(this Descriptor descriptor, string name, int @default) =>
            descriptor.Meta != null && descriptor.Meta.TryGetValue(name, out var json) && json != null && json.IsInt ? json.AsInt : @default;
    }
}
