using Json;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace JsonDiff
{
    public record JsonPatchChunk(JsonPath Path, ImmutableJson? Value, int? Index = null);

    public class JsonPatch
    {
        public List<JsonPatchChunk> Chunks { get; } = new List<JsonPatchChunk>();

        public string ToJavaScript(JsonPath? prefix = null)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < Chunks.Count; i++)
            {
                var chunk = Chunks[i];
                var path = prefix == null ? chunk.Path : JsonPath.Join(prefix, chunk.Path);
                if (chunk.Value != null)
                {
                    if (chunk.Index.HasValue)
                        sb.AppendFormat(CultureInfo.InvariantCulture, "{0}.splice({1}, 0, {2:4});", path, chunk.Index.Value, chunk.Value);
                    else
                        sb.AppendFormat(CultureInfo.InvariantCulture, "{0} = {1:4};", path, chunk.Value);
                }
                else
                {
                    if (chunk.Index.HasValue)
                    {
                        var j = i;
                        while (i < Chunks.Count - 1)
                        {
                            var nextChunk = Chunks[i + 1];
                            if (nextChunk.Path.Equals(chunk.Path) && nextChunk.Value == null && nextChunk.Index == chunk.Index)
                                i++;
                            else
                                break;
                        }
                        sb.AppendFormat(CultureInfo.InvariantCulture, "{0}.splice({1}, {2});", path, chunk.Index.Value, i - j + 1);
                    }
                    else
                        sb.AppendFormat(CultureInfo.InvariantCulture, "delete {0};", path);
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public ImmutableJson Apply(ImmutableJson json)
        {
            var builder = new JsonBuilder(json);
            Apply(builder);
            return builder.ToImmutable();
        }

        public void Apply(JsonBuilder container)
        {
            foreach (var chunk in Chunks)
                ApplyChunk(container, chunk);
        }

        public static bool ApplyChunk(JsonBuilder container, JsonPatchChunk chunk)
        {
            try
            {
                if (chunk.Index.HasValue)
                {
                    if (chunk.Value != null)
                        container.Fetch(chunk.Path).AsArray.Insert(chunk.Index.Value, new JsonBuilder(chunk.Value));
                    else
                        container.Delete(chunk.Path.AppendArray(chunk.Index.Value));
                }
                else
                {
                    if (chunk.Value != null)
                        container.Update(chunk.Path, new JsonBuilder(chunk.Value));
                    else
                        container.Delete(chunk.Path);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static JsonPatch FromDiff(JsonDiff diff, bool left = false)
        {
            var patch = new JsonPatch();
            FillPatch(patch, diff, JsonPath.Empty, left);
            return patch;
        }

        static void FillPatch(JsonPatch patch, JsonDiff diff, JsonPath path, bool left)
        {
            switch (diff)
            {
                case JsonDiffArray diffArray:
                    FillPatchArray(patch, diffArray, path, left);
                    break;
                case JsonDiffObject diffObject:
                    FillPatchObject(patch, diffObject, path, left);
                    break;
                case JsonDiffDifferent d:
                    {
                        var chunk = new JsonPatchChunk(path, left ? d.Left : d.Right);
                        patch.Chunks.Add(chunk);
                        break;
                    }
                default:
                    return;
            }
        }

        static void FillPatchObject(JsonPatch patch, JsonDiffObject diff, JsonPath path, bool left)
        {
            foreach (var pair in diff.Items)
            {
                FillPatch(patch, pair.Value, path.AppendObject(pair.Key), left);
            }
        }

        static void FillPatchArray(JsonPatch patch, JsonDiffArray diff, JsonPath path, bool left)
        {
            int i = 0;
            foreach (var item in diff.Items)
            {
                if (item is JsonDiffArray diffArray)
                    FillPatchArray(patch, diffArray, path.AppendArray(i), left);
                else if (item is JsonDiffObject diffObject)
                    FillPatchObject(patch, diffObject, path.AppendArray(i), left);
                else if (item is JsonDiffDifferent d)
                {
                    var oldJson = left ? d.Right : d.Left;
                    var newJson = left ? d.Left : d.Right;
                    if (oldJson != null && newJson != null)
                    {
                        patch.Chunks.Add(new JsonPatchChunk(path.AppendArray(i), newJson));
                    }
                    else
                    {
                        patch.Chunks.Add(new JsonPatchChunk(path, newJson, i));
                        if (newJson == null)
                            i--;
                    }
                }
                i++;
            }
        }
    }
}
