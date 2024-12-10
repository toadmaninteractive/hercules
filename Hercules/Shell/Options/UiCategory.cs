using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Hercules.Shell
{
    public readonly record struct UiCategory(string Name, int Index)
    {
        public override string ToString()
        {
            return $"{Name}#{Index}";
        }
    }

    public sealed class UiCategoryPath : IEquatable<UiCategoryPath?>
    {
        private readonly List<UiCategory> parts;

        public IReadOnlyList<UiCategory> Parts => parts;

        public UiCategoryPath(string category)
        {
            var partStrs = category.Split('/');
            parts = new List<UiCategory>(partStrs.Length);
            foreach (var partStr in partStrs)
            {
                var hashIndex = partStr.IndexOf('#', StringComparison.Ordinal);
                if (hashIndex < 0)
                    parts.Add(new UiCategory(partStr, 0));
                else
                    parts.Add(new UiCategory(partStr.Substring(0, hashIndex), int.Parse(partStr.AsSpan(hashIndex + 1), NumberStyles.Integer, CultureInfo.InvariantCulture)));
            }
        }

        public bool Equals(UiCategoryPath? other)
        {
            if (other == null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (this.parts.Count != other.parts.Count)
                return false;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] != other.parts[i])
                    return false;
            }
            return true;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            bool isFirst = true;
            foreach (var part in Parts)
            {
                if (!isFirst)
                    sb.Append("/");
                isFirst = false;
                sb.Append(part);
            }
            return sb.ToString();
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as UiCategoryPath);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var part in parts)
                hash.Add(part.GetHashCode());
            return hash.ToHashCode();
        }
    }
}
