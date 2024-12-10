using Json;
using System.Collections.Generic;

namespace JsonDiff
{
    public abstract record JsonDiff;

    public record JsonDiffObject(IReadOnlyDictionary<string, JsonDiff> Items) : JsonDiff;

    public record JsonDiffArray(IReadOnlyList<JsonDiff> Items) : JsonDiff;

    public record JsonDiffSame(ImmutableJson Value) : JsonDiff;

    public record JsonDiffDifferent(ImmutableJson? Left, ImmutableJson? Right) : JsonDiff;
}
