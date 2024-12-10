using Hercules;
using Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonDiff
{
    public record JsonDiffChunk(JsonPath Path, ImmutableJson? Left, ImmutableJson? Right);

    public static class JsonDiffHelper
    {
        public static void ItemCount(IEnumerable<JsonDiff> items, out int left, out int right)
        {
            left = 0;
            right = 0;
            foreach (var item in items)
            {
                if (item is JsonDiffDifferent diff)
                {
                    if (diff.Left != null)
                        left++;
                    if (diff.Right != null)
                        right++;
                }
                else
                {
                    left++;
                    right++;
                }
            }
        }

        public static void DecItemCount(JsonDiff item, ref int left, ref int right)
        {
            if (item is JsonDiffDifferent diff)
            {
                if (diff.Left != null)
                    left--;
                if (diff.Right != null)
                    right--;
            }
            else
            {
                left--;
                right--;
            }
        }

        public static JsonDiff Swap(this JsonDiff diff)
        {
            return diff switch
            {
                JsonDiffArray array => new JsonDiffArray(array.Items.Select(Swap).ToList()),
                JsonDiffObject obj => new JsonDiffObject(obj.Items.ToDictionary(pair => pair.Key, pair => pair.Value.Swap())),
                _ => diff
            };
        }

        public static ImmutableJson? Left(JsonDiff diff)
        {
            return GetJson(diff, true);
        }

        public static ImmutableJson? Right(JsonDiff diff)
        {
            return GetJson(diff, false);
        }

        public static ImmutableJson? GetJson(JsonDiff diff, bool left)
        {
            switch (diff)
            {
                case JsonDiffArray array:
                    return new JsonArray(array.Items.Select(d => GetJson(d, left)).WhereNotNull());
                case JsonDiffObject obj:
                    {
                        var json = new JsonObject();
                        foreach (var pair in obj.Items)
                        {
                            var child = GetJson(pair.Value, left);
                            if (child != null)
                                json.Add(pair.Key, child);
                        }

                        return json;
                    }
                case JsonDiffSame same:
                    return same.Value;
                case JsonDiffDifferent d:
                    {
                        if (left)
                            return d.Left;
                        else
                            return d.Right;
                    }
                default:
                    throw new ArgumentException("Unknown JsonDiff type", nameof(diff));
            }
        }

        public static IReadOnlyList<JsonDiffChunk> GetChunks(this JsonDiff diff)
        {
            var chunks = new List<JsonDiffChunk>();
            FillChunks(chunks, diff, JsonPath.Empty);
            return chunks;
        }

        private static void FillChunks(List<JsonDiffChunk> chunks, JsonDiff diff, JsonPath path)
        {
            if (diff is JsonDiffArray diffArray)
            {
                FillChunksArray(chunks, diffArray, path);
            }
            else if (diff is JsonDiffObject diffObject)
            {
                FillChunksObject(chunks, diffObject, path);
            }
            else if (diff is JsonDiffDifferent d)
            {
                chunks.Add(new JsonDiffChunk(path, d.Left, d.Right));
            }
        }

        private static void FillChunksObject(List<JsonDiffChunk> chunks, JsonDiffObject diff, JsonPath path)
        {
            foreach (var pair in diff.Items)
                FillChunks(chunks, pair.Value, path.AppendObject(pair.Key));
        }

        private static void FillChunksArray(List<JsonDiffChunk> chunks, JsonDiffArray diff, JsonPath path)
        {
            int i = 0;
            foreach (var item in diff.Items)
            {
                if (item is JsonDiffArray diffArray)
                    FillChunksArray(chunks, diffArray, path.AppendArray(i));
                else if (item is JsonDiffObject diffObject)
                    FillChunksObject(chunks, diffObject, path.AppendArray(i));
                else if (item is JsonDiffDifferent d)
                    chunks.Add(new JsonDiffChunk(path.AppendArray(i), d.Left, d.Right));
                i++;
            }
        }
    }
}
