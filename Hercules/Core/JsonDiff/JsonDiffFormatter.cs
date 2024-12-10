using Json;
using System.Collections.Generic;
using System.Text;

namespace JsonDiff
{
    public class JsonDiffFormatter
    {
        private const string IndentString = "    ";

        public StringBuilder Builder { get; private set; }
        public List<DiffLineStyle> Lines { get; private set; }

        public JsonDiffFormatter()
        {
            this.Builder = new StringBuilder();
            this.Lines = new List<DiffLineStyle>();
        }

        void WriteLine(DiffLineStyle mode, string? key, string str, bool comma, int indent)
        {
            for (int i = 0; i < indent; i++)
                Builder.Append(IndentString);
            if (key != null)
            {
                Builder.Append('"');
                Builder.Append(key);
                Builder.Append("\": ");
            }
            Builder.Append(str);
            if (comma)
                Builder.Append(',');
            Builder.AppendLine();
            Lines.Add(mode);
        }

        void Format(DiffLineStyle mode, ImmutableJson json, bool comma, int indent, string? key = null)
        {
            if (json.IsArray)
            {
                WriteLine(mode, key, "[", false, indent);
                for (int i = 0; i < json.AsArray.Count; i++)
                {
                    bool childComma = i < json.AsArray.Count - 1;
                    Format(mode, json[i], childComma, indent + 1);
                }
                WriteLine(mode, null, "]", comma, indent);
            }
            else if (json.IsObject)
            {
                WriteLine(mode, key, "{", false, indent);
                int i = 0;
                foreach (var pair in json.AsObject)
                {
                    bool childComma = i < json.AsObject.Count - 1;
                    i++;
                    Format(mode, pair.Value, childComma, indent + 1, pair.Key);
                }
                WriteLine(mode, null, "}", comma, indent);
            }
            else
            {
                WriteLine(mode, key, json.ToString(), comma, indent);
            }
        }

        void Process(JsonDiff diff, bool leftComma, bool rightComma, int indent, string? key = null)
        {
            if (diff is JsonDiffSame diffSame)
            {
                var json = diffSame.Value;
                Format(DiffLineStyle.Normal, json, rightComma, indent, key);
            }
            else if (diff is JsonDiffDifferent different)
            {
                if (different.Left != null)
                    Format(DiffLineStyle.Deleted, different.Left, leftComma, indent, key);
                if (different.Right != null)
                    Format(DiffLineStyle.Added, different.Right, rightComma, indent, key);
            }
            if (diff is JsonDiffArray array)
            {
                JsonDiffHelper.ItemCount(array.Items, out var leftCount, out var rightCount);
                WriteLine(DiffLineStyle.Normal, key, "[", false, indent);
                foreach (var item in array.Items)
                {
                    JsonDiffHelper.DecItemCount(item, ref leftCount, ref rightCount);
                    Process(item, leftCount > 0, rightCount > 0, indent + 1);
                }
                WriteLine(DiffLineStyle.Normal, null, "]", rightComma, indent);
            }
            else if (diff is JsonDiffObject obj)
            {
                JsonDiffHelper.ItemCount(obj.Items.Values, out var leftCount, out var rightCount);
                WriteLine(DiffLineStyle.Normal, key, "{", false, indent);
                foreach (var pair in obj.Items)
                {
                    JsonDiffHelper.DecItemCount(pair.Value, ref leftCount, ref rightCount);
                    Process(pair.Value, leftCount > 0, rightCount > 0, indent + 1, pair.Key);
                }
                WriteLine(DiffLineStyle.Normal, null, "}", rightComma, indent);
            }
        }

        public void Process(JsonDiff diff)
        {
            Process(diff, false, false, 0);
        }
    }
}
