using Json;
using System.Collections.Generic;
using System.Linq;

namespace JsonDiff
{
    public class JsonDiffEngine
    {
        static void CopyArrayRange(List<JsonDiff> array, IReadOnlyList<ImmutableJson> items, int fromIndex, int count, bool left)
        {
            if (count == 0)
                return;
            var range = items.Skip(fromIndex).Take(count);
            array.AddRange(range.Select(json => new JsonDiffDifferent(left ? json : null, left ? null : json)));
        }

        JsonDiff ProcessArray(ImmutableJson left, ImmutableJson right)
        {
            var diff = new List<JsonDiff>();
            int leftIndex = 0;
            int rightIndex = 0;
            while (leftIndex < left.AsArray.Count && rightIndex < right.AsArray.Count)
            {
                var leftItem = left[leftIndex];
                var rightItem = right[rightIndex];
                var rightMatch = right.AsArray.FindIndex(rightIndex, json => json.Equals(leftItem));
                var leftMatch = left.AsArray.FindIndex(leftIndex, json => json.Equals(rightItem));
                if (rightMatch < 0 && leftMatch < 0)
                {
                    diff.Add(Process(leftItem, rightItem));
                    leftIndex++;
                    rightIndex++;
                }
                else
                {
                    bool useLeftMatch = leftMatch >= 0 && (rightMatch < 0 || rightMatch - rightIndex > leftMatch - leftIndex);
                    if (useLeftMatch)
                    {
                        CopyArrayRange(diff, left.AsArray, leftIndex, leftMatch - leftIndex, true);
                        diff.Add(new JsonDiffSame(rightItem));
                        leftIndex = leftMatch + 1;
                        rightIndex++;
                    }
                    else
                    {
                        CopyArrayRange(diff, right.AsArray, rightIndex, rightMatch - rightIndex, false);
                        diff.Add(new JsonDiffSame(leftItem));
                        rightIndex = rightMatch + 1;
                        leftIndex++;
                    }
                }
            }
            if (leftIndex < left.AsArray.Count)
                CopyArrayRange(diff, left.AsArray, leftIndex, left.AsArray.Count - leftIndex, true);
            if (rightIndex < right.AsArray.Count)
                CopyArrayRange(diff, right.AsArray, rightIndex, right.AsArray.Count - rightIndex, false);
            return new JsonDiffArray(diff);
        }

        JsonDiff ProcessObject(ImmutableJson left, ImmutableJson right, HashSet<string>? ignoreKeys = null)
        {
            var keys = left.AsObject.Keys.Concat(right.AsObject.Keys).Distinct().Where(key => ignoreKeys == null || !ignoreKeys.Contains(key)).OrderBy(s => s).ToList();
            var diff = new Dictionary<string, JsonDiff>();
            foreach (var key in keys)
            {
                var leftChild = left.AsObject.GetValueOrNull(key);
                var rightChild = right.AsObject.GetValueOrNull(key);

                if (leftChild.IsNull && rightChild.IsNull)
                    continue;

                if (leftChild.IsNull)
                    diff.Add(key, new JsonDiffDifferent(null, rightChild));
                else if (rightChild.IsNull)
                    diff.Add(key, new JsonDiffDifferent(leftChild, null));
                else
                    diff.Add(key, Process(leftChild, rightChild));
            }
            return new JsonDiffObject(diff);
        }

        public JsonDiff Process(ImmutableJson left, ImmutableJson right, IEnumerable<string>? ignoreKeys = null)
        {
            if (left.Equals(right))
                return new JsonDiffSame(left);
            else if (left.IsArray && right.IsArray)
                return ProcessArray(left, right);
            else if (left.IsObject && right.IsObject)
                return ProcessObject(left, right, ignoreKeys == null ? null : new HashSet<string>(ignoreKeys));
            else
                return new JsonDiffDifferent(left, right);
        }
    }
}
