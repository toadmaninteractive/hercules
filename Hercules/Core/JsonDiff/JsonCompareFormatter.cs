using Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace JsonDiff
{
    public class JsonCompareFormatter
    {
        [Flags]
        public enum Targets
        {
            None = 0,
            Left = 1,
            Right = 2,
            Both = 3,
        }

        private const string IndentString = "    ";

        public StringBuilder LeftBuilder { get; }
        public StringBuilder RightBuilder { get; }
        public List<DiffLineStyle> LeftLines { get; }
        public List<DiffLineStyle> RightLines { get; }

        public JsonCompareFormatter()
        {
            this.LeftBuilder = new StringBuilder();
            this.RightBuilder = new StringBuilder();
            this.LeftLines = new List<DiffLineStyle>();
            this.RightLines = new List<DiffLineStyle>();
        }

        static string FormatLine(string? key, string str, int indent, bool comma)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < indent; i++)
                sb.Append(IndentString);
            if (key != null)
            {
                sb.Append('"');
                sb.Append(key);
                sb.Append("\": ");
            }
            sb.Append(str);
            if (comma)
                sb.Append(',');
            return sb.ToString();
        }

        void WriteLine(Targets target, string? key, string str, bool leftComma, bool rightComma, int indent)
        {
            if (target.HasFlag(Targets.Left))
            {
                var leftStr = FormatLine(key, str, indent, leftComma);
                LeftBuilder.AppendLine(leftStr);
                LeftLines.Add(target == Targets.Both ? DiffLineStyle.Normal : DiffLineStyle.Added);
            }
            if (target.HasFlag(Targets.Right))
            {
                var rightStr = FormatLine(key, str, indent, rightComma);
                RightBuilder.AppendLine(rightStr);
                RightLines.Add(target == Targets.Both ? DiffLineStyle.Normal : DiffLineStyle.Added);
            }
        }

        void Format(Targets target, ImmutableJson json, bool leftComma, bool rightComma, int indent, string? key = null)
        {
            if (json.IsArray)
            {
                WriteLine(target, key, "[", false, false, indent);
                for (int i = 0; i < json.AsArray.Count; i++)
                {
                    bool childComma = i < json.AsArray.Count - 1;
                    Format(target, json[i], childComma, childComma, indent + 1);
                }
                WriteLine(target, null, "]", leftComma, rightComma, indent);
            }
            else if (json.IsObject)
            {
                WriteLine(target, key, "{", false, false, indent);
                int i = 0;
                foreach (var pair in json.AsObject)
                {
                    bool childComma = i < json.AsObject.Count - 1;
                    i++;
                    Format(target, pair.Value, childComma, childComma, indent + 1, pair.Key);
                }
                WriteLine(target, null, "}", leftComma, rightComma, indent);
            }
            else
            {
                WriteLine(target, key, json.ToString(), leftComma, rightComma, indent);
            }
        }

        void SkipLines()
        {
            var delta = LeftLines.Count - RightLines.Count;
            if (delta < 0)
            {
                for (int i = 0; i < -delta; i++)
                {
                    LeftBuilder.AppendLine();
                    LeftLines.Add(DiffLineStyle.Skip);
                }
            }
            else if (delta > 0)
            {
                for (int i = 0; i < delta; i++)
                {
                    RightBuilder.AppendLine();
                    RightLines.Add(DiffLineStyle.Skip);
                }
            }
        }

        void Process(JsonDiff diff, bool leftComma, bool rightComma, int indent, string? key = null)
        {
            switch (diff)
            {
                case JsonDiffSame same:
                    {
                        var json = same.Value;
                        Format(Targets.Both, json, leftComma, rightComma, indent, key);
                    }
                    break;
                case JsonDiffDifferent different:
                    {
                        if (different.Left != null)
                            Format(Targets.Left, different.Left, leftComma, rightComma, indent, key);
                        if (different.Right != null)
                            Format(Targets.Right, different.Right, leftComma, rightComma, indent, key);
                        SkipLines();
                    }
                    break;
                case JsonDiffArray array:
                    {
                        JsonDiffHelper.ItemCount(array.Items, out var leftCount, out var rightCount);
                        WriteLine(Targets.Both, key, "[", false, false, indent);
                        foreach (var item in array.Items)
                        {
                            JsonDiffHelper.DecItemCount(item, ref leftCount, ref rightCount);
                            Process(item, leftCount > 0, rightCount > 0, indent + 1);
                        }

                        WriteLine(Targets.Both, null, "]", leftComma, rightComma, indent);
                    }
                    break;
                case JsonDiffObject obj:
                    {
                        JsonDiffHelper.ItemCount(obj.Items.Values, out var leftCount, out var rightCount);
                        WriteLine(Targets.Both, key, "{", false, false, indent);
                        foreach (var pair in obj.Items)
                        {
                            JsonDiffHelper.DecItemCount(pair.Value, ref leftCount, ref rightCount);
                            Process(pair.Value, leftCount > 0, rightCount > 0, indent + 1, pair.Key);
                        }

                        WriteLine(Targets.Both, null, "}", leftComma, rightComma, indent);
                    }
                    break;
            }
        }

        public void Process(JsonDiff diff)
        {
            Process(diff, false, false, 0);
        }
    }
}
