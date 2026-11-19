using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Json
{
    public abstract class JsonPathNode : IEquatable<JsonPathNode>
    {
        public abstract ImmutableJson? Fetch(ImmutableJson data);
        public abstract ImmutableJson Update(ImmutableJson data, ImmutableJson update);
        public abstract ImmutableJson ForceUpdate(ImmutableJson data, ImmutableJson update);
        public abstract ImmutableJson Delete(ImmutableJson data);

        internal abstract JsonBuilder? BuilderFetch(JsonBuilder data);
        internal abstract JsonBuilder BuilderEnsure(JsonBuilder data);
        internal abstract void BuilderUpdate(JsonBuilder data, JsonBuilder update);
        internal abstract void BuilderDelete(JsonBuilder data);

        public abstract void Format(StringBuilder builder);
        public abstract bool Equals(JsonPathNode? other);
    }

    public class JsonObjectPathNode : JsonPathNode
    {
        public string Key { get; private set; }

        public JsonObjectPathNode(string key)
        {
            this.Key = key;
        }

        public override void Format(StringBuilder builder)
        {
            builder.Append(".");
            builder.Append(Key);
        }

        public override ImmutableJson? Fetch(ImmutableJson data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (!data.IsObject)
                return null;
            if (data.AsObject.TryGetValue(Key, out var result))
                return result;
            else
                return null;
        }

        public override ImmutableJson Update(ImmutableJson data, ImmutableJson update)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (!data.IsObject)
                throw new ArgumentOutOfRangeException(nameof(data), "JSON data is not an object");
            var obj = new JsonObject(data.AsObject);
            obj[Key] = update;
            return obj;
        }

        public override ImmutableJson ForceUpdate(ImmutableJson data, ImmutableJson update)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            JsonObject obj;
            if (data.IsObject)
                obj = new JsonObject(data.AsObject);
            else
                obj = new JsonObject();

            obj[Key] = update;
            return obj;
        }

        public override ImmutableJson Delete(ImmutableJson data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (!data.IsObject)
                throw new ArgumentOutOfRangeException(nameof(data), "JSON data is not an object");
            var res = new JsonObject(data.AsObject);
            res.Remove(Key);
            return res;
        }

        internal override JsonBuilder? BuilderFetch(JsonBuilder data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (!data.IsObject)
                return null;
            if (data.AsObject.TryGetValue(Key, out var result))
                return result;
            else
                return null;
        }

        internal override JsonBuilder BuilderEnsure(JsonBuilder data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            return data.GetOrAdd(Key);
        }

        internal override void BuilderUpdate(JsonBuilder data, JsonBuilder update)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            data.AsObject[Key] = update;
        }

        internal override void BuilderDelete(JsonBuilder data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            data.AsObject.Remove(Key);
        }

        public override bool Equals(JsonPathNode? other)
        {
            if (other is JsonObjectPathNode)
                return Key == ((JsonObjectPathNode)other).Key;
            else
                return false;
        }

        public override bool Equals(object? obj)
        {
            return this.Equals(obj as JsonPathNode);
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }
    }

    public class JsonObjectKeyPathNode : JsonPathNode
    {
        public string Key { get; private set; }

        public JsonObjectKeyPathNode(string key)
        {
            this.Key = key;
        }

        public override void Format(StringBuilder builder)
        {
            builder.Append(".");
            builder.Append(Key);
        }

        public override ImmutableJson? Fetch(ImmutableJson data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (!data.IsObject)
                return null;
            if (data.AsObject.ContainsKey(Key))
                return Key;
            else
                return null;
        }

        static string ToKey(ImmutableJson json)
        {
            return json.IsString ? json.AsString : json.ToString();
        }

        static bool IsKey(ImmutableJson json)
        {
            return json.IsString || json.IsInt;
        }

        public override ImmutableJson Update(ImmutableJson data, ImmutableJson update)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (update == null)
                throw new ArgumentNullException(nameof(update));
            if (!data.IsObject)
                throw new ArgumentOutOfRangeException(nameof(data), "JSON data is not an object");
            if (!IsKey(update))
                throw new ArgumentOutOfRangeException(nameof(update), "JSON data cannot be used as an object key");

            var res = new JsonObject(data.AsObject);
            res.Remove(Key);
            res[ToKey(update)] = data[Key];
            return res;
        }

        public override ImmutableJson ForceUpdate(ImmutableJson data, ImmutableJson update)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (update == null)
                throw new ArgumentNullException(nameof(update));
            if (!IsKey(update))
                throw new ArgumentOutOfRangeException(nameof(update), "JSON data cannot be used as an object key");

            JsonObject res;
            if (data.IsObject)
                res = new JsonObject(data.AsObject);
            else
                res = new JsonObject();
            if (!res.TryGetValue(Key, out var value))
                value = ImmutableJson.Null;
            res.Remove(Key);
            res[ToKey(update)] = value;
            return res;
        }

        public override ImmutableJson Delete(ImmutableJson data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (!data.IsObject)
                throw new ArgumentOutOfRangeException(nameof(data), "JSON data is not an object");

            var res = new JsonObject(data.AsObject);
            res.Remove(Key);
            return res;
        }

        internal override JsonBuilder? BuilderFetch(JsonBuilder data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (!data.IsObject)
                return null;
            if (data.GetOrCreateObject().TryGetValue(Key, out var result))
                return result;
            else
                return null;
        }

        internal override JsonBuilder BuilderEnsure(JsonBuilder data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var obj = data.GetOrCreateObject();
            if (obj.ContainsKey(Key))
                return new JsonBuilder(Key);

            return JsonBuilder.Null;
        }

        internal override void BuilderUpdate(JsonBuilder data, JsonBuilder update)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (update == null)
                throw new ArgumentNullException(nameof(update));
            var newKeyJson = update.ToImmutable();
            if (!IsKey(newKeyJson))
                throw new ArgumentOutOfRangeException(nameof(update), "JSON data cannot be used as an object key");
            var obj = data.AsObject;
            var newKey = ToKey(newKeyJson);
            if (newKey != Key)
            {
                obj[newKey] = obj[Key];
                obj.Remove(Key);
            }
        }

        internal override void BuilderDelete(JsonBuilder data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            data.AsObject.Remove(Key);
        }

        public override bool Equals(JsonPathNode? other)
        {
            if (other is JsonObjectKeyPathNode)
                return Key == ((JsonObjectKeyPathNode)other).Key;
            else
                return false;
        }

        public override bool Equals(object? obj)
        {
            return this.Equals(obj as JsonPathNode);
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }
    }

    public class JsonArrayPathNode : JsonPathNode
    {
        public int Index { get; private set; }

        public JsonArrayPathNode(int index)
        {
            Index = index;
        }

        public override void Format(StringBuilder builder)
        {
            builder.AppendFormat("[{0}]", Index);
        }

        public override ImmutableJson? Fetch(ImmutableJson data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (!data.IsArray)
                return null;
            if (Index < data.AsArray.Count)
                return data[Index];
            else
                return null;
        }

        public override ImmutableJson Update(ImmutableJson data, ImmutableJson update)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (update == null)
                throw new ArgumentNullException(nameof(update));
            if (!data.IsArray)
                throw new ArgumentOutOfRangeException(nameof(data), "JSON data is not an array");
            var res = new JsonArray(data.AsArray);
            res[Index] = update;
            return res;
        }

        public override ImmutableJson ForceUpdate(ImmutableJson data, ImmutableJson update)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (update == null)
                throw new ArgumentNullException(nameof(update));
            JsonArray res;
            if (data.IsArray)
                res = new JsonArray(data.AsArray);
            else
                res = new JsonArray();
            while (res.Count <= Index)
                res.Add(ImmutableJson.Null);
            res[Index] = update;
            return res;
        }

        public override ImmutableJson Delete(ImmutableJson data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (!data.IsArray)
                throw new ArgumentOutOfRangeException(nameof(data), "JSON data is not an object");
            var res = new JsonArray(data.AsArray);
            res.RemoveAt(Index);
            return res;
        }

        internal override JsonBuilder? BuilderFetch(JsonBuilder data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (!data.IsArray)
                return null;
            var arr = data.AsArray;
            if (Index < arr.Count)
                return arr[Index];
            else
                return null;
        }

        internal override JsonBuilder BuilderEnsure(JsonBuilder data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            return data.GetOrAdd(Index);
        }

        internal override void BuilderUpdate(JsonBuilder data, JsonBuilder update)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            data.AsArray[Index] = update;
        }

        internal override void BuilderDelete(JsonBuilder data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            data.AsArray.RemoveAt(Index);
        }

        public override bool Equals(JsonPathNode? other)
        {
            if (other is JsonArrayPathNode)
                return Index == ((JsonArrayPathNode)other).Index;
            else
                return false;
        }

        public override bool Equals(object? obj)
        {
            return this.Equals(obj as JsonPathNode);
        }

        public override int GetHashCode()
        {
            return Index.GetHashCode();
        }
    }

    public class JsonPath : IEquatable<JsonPath>
    {
        private enum JsonPathToken
        {
            None,
            Key,
            Index,
            Dot,
        }

        public JsonPathNode[] Nodes { get; }

        public static readonly JsonPath Empty = new JsonPath(new JsonPathNode[0]);

        static readonly char[] Separators = new char[] { '.', '[', ']' };

        JsonPath(JsonPathNode[] nodes)
        {
            Nodes = nodes;
        }

        public JsonPath(params string[] keys)
        {
            Nodes = keys.Select(p => new JsonObjectPathNode(p)).ToArray();
        }

        public static JsonPath Join(params JsonPath[] paths)
        {
            return new JsonPath(paths.SelectMany(p => p.Nodes).ToArray());
        }

        public JsonPathNode? Head => Nodes.Length == 0 ? null : Nodes[0];

        public JsonPath Tail
        {
            get
            {
                if (Nodes.Length < 1)
                    throw new IndexOutOfRangeException();
                var clone = new JsonPathNode[Nodes.Length - 1];
                Array.Copy(Nodes, 1, clone, 0, clone.Length);
                return new JsonPath(clone);
            }
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (var node in Nodes)
                node.Format(builder);
            var s = builder.ToString();
            if (s.StartsWith("."))
                s = s.Substring(1);
            return s;
        }

        public bool Equals(JsonPath? other)
        {
            if (other == null)
                return false;
            return Enumerable.SequenceEqual(Nodes, other.Nodes);
        }

        public override bool Equals(object? obj)
        {
            return this.Equals(obj as JsonPath);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                foreach (var item in Nodes)
                {
                    hash = hash * 23 + ((item != null) ? item.GetHashCode() : 0);
                }

                return hash;
            }
        }

        JsonPath Append(JsonPathNode node)
        {
            var clone = new JsonPathNode[Nodes.Length + 1];
            Array.Copy(Nodes, 0, clone, 0, Nodes.Length);
            clone[Nodes.Length] = node;
            return new JsonPath(clone);
        }

        public JsonPath AppendObject(string key)
        {
            return Append(new JsonObjectPathNode(key));
        }

        public JsonPath AppendObjectKey(string key)
        {
            return Append(new JsonObjectKeyPathNode(key));
        }

        public JsonPath AppendArray(int index)
        {
            return Append(new JsonArrayPathNode(index));
        }

        internal ImmutableJson InternalForceUpdate(ImmutableJson data, int index, ImmutableJson update)
        {
            var node = Nodes[index];
            if (index == Nodes.Length - 1)
                return node.ForceUpdate(data, update);
            return node.ForceUpdate(data, InternalForceUpdate(node.Fetch(data) ?? ImmutableJson.Null, index + 1, update));
        }

        internal ImmutableJson InternalUpdate(ImmutableJson data, int index, ImmutableJson update)
        {
            var node = Nodes[index];
            if (index == Nodes.Length - 1)
                return node.Update(data, update);
            return node.Update(data, InternalUpdate(node.Fetch(data), index + 1, update));
        }

        internal ImmutableJson InternalDelete(ImmutableJson data, int index)
        {
            var node = Nodes[index];
            if (index == Nodes.Length - 1)
                return node.Delete(data);
            return node.Update(data, InternalDelete(node.Fetch(data), index + 1));
        }

        public static JsonPath Parse(string path)
        {
            List<JsonPathNode> nodes = new List<JsonPathNode>();
            var pos = 0;
            var lastToken = JsonPathToken.None;
            while (pos < path.Length)
            {
                var i = path.IndexOfAny(Separators, pos);
                if (i != pos)
                {
                    if (lastToken == JsonPathToken.Index)
                        throw new ArgumentException("Invalid path", nameof(path));
                    var key = i < 0 ? path.Substring(pos) : path.Substring(pos, i - pos);
                    nodes.Add(new JsonObjectPathNode(key));
                    lastToken = JsonPathToken.Key;
                    if (i < 0)
                        break;
                }
                var c = path[i];
                if (c == '[')
                {
                    var i1 = path.IndexOf(']', i + 1);
                    if (i1 < 0)
                        throw new ArgumentException("Invalid path", nameof(path));
                    var index = int.Parse(path.Substring(i + 1, i1 - i - 1));
                    nodes.Add(new JsonArrayPathNode(index));
                    pos = i1 + 1;
                    lastToken = JsonPathToken.Index;
                }
                else if (c == ']')
                {
                    throw new ArgumentException("Invalid path", nameof(path));
                }
                else // if (c == '.')
                {
                    if (lastToken == JsonPathToken.Dot || lastToken == JsonPathToken.None)
                        throw new ArgumentException("Invalid path", nameof(path));
                    pos = i + 1;
                    lastToken = JsonPathToken.Dot;
                }
            }
            if (lastToken == JsonPathToken.Dot)
                throw new ArgumentException("Invalid path", nameof(path));
            return new JsonPath(nodes.ToArray());
        }
    }
}
