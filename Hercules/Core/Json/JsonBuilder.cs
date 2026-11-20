using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Json
{
    public class JsonBuilder
    {
        object? data;

        #region Getters

        public JsonType Type
        {
            get
            {
                if (IsValue)
                    return AsValue.Type;
                if (IsArray)
                    return JsonType.Array;
                if (IsObject)
                    return JsonType.Object;
                return JsonType.Null;
            }
        }

        bool IsValue => data is ImmutableJson;

        ImmutableJson AsValue => (ImmutableJson)data!;

        public bool IsArray => data is List<JsonBuilder> || data is ImmutableJsonArray;

        public List<JsonBuilder> AsArray
        {
            get
            {
                if (data is List<JsonBuilder> list)
                    return list;

                if (data is ImmutableJsonArray json)
                {
                    var newList = json.Select(el => new JsonBuilder(el)).ToList();
                    data = newList;
                    return newList;
                }

                throw new InvalidCastException("JSON data is not an Array");
            }
        }

        public bool IsObject => data is Dictionary<string, JsonBuilder> || data is ImmutableJsonObject;

        public Dictionary<string, JsonBuilder> AsObject
        {
            get
            {
                if (data is Dictionary<string, JsonBuilder> dict)
                    return dict;

                if (data is ImmutableJsonObject json)
                {
                    var newDict = json.ToDictionary(pair => pair.Key, pair => new JsonBuilder(pair.Value));
                    data = newDict;
                    return newDict;
                }

                throw new InvalidCastException("JSON data is not an Object");
            }
        }

        #endregion

        #region Operators

        public JsonBuilder this[int index]
        {
            get { return Get(index); }
            set { GetOrAdd(index).Set(value); }
        }

        public JsonBuilder this[string key]
        {
            get { return Get(key); }
            set { GetOrAdd(key).Set(value); }
        }

        #endregion

        #region API

        public List<JsonBuilder> GetOrCreateArray()
        {
            switch (data)
            {
                case List<JsonBuilder> datalist:
                    return datalist;
                case ImmutableJsonArray json:
                    var newList = json.Select(el => new JsonBuilder(el)).ToList();
                    data = newList;
                    return newList;
                default:
                    var list = new List<JsonBuilder>();
                    data = list;
                    return list;
            }
        }

        public Dictionary<string, JsonBuilder> GetOrCreateObject()
        {
            switch (data)
            {
                case Dictionary<string, JsonBuilder> datadict:
                    return datadict;
                case ImmutableJsonObject json:
                    var newDict = json.ToDictionary(pair => pair.Key, pair => new JsonBuilder(pair.Value));
                    data = newDict;
                    return newDict;
                default:
                    var dict = new Dictionary<string, JsonBuilder>();
                    data = dict;
                    return dict;
            }
        }

        public void Set(ImmutableJson value)
        {
            ArgumentNullException.ThrowIfNull(value);
            data = value;
        }

        public void Set(JsonBuilder value)
        {
            data = value.data;
        }

        public JsonBuilder Get(int index)
        {
            return AsArray[index];
        }

        public JsonBuilder GetOrAdd(int index)
        {
            var arr = GetOrCreateArray();
            while (arr.Count <= index)
                arr.Add(Null);
            return arr[index];
        }

        public JsonBuilder Get(string key)
        {
            return AsObject[key];
        }

        public JsonBuilder GetOrAdd(string key)
        {
            var obj = GetOrCreateObject();
            if (obj.TryGetValue(key, out var result))
                return result;
            result = Null;
            obj[key] = result;
            return result;
        }

        #endregion

        #region Path

        public JsonBuilder Fetch(JsonPath path)
        {
            ArgumentNullException.ThrowIfNull(path);

            JsonBuilder? res = this;
            foreach (var node in path.Nodes)
            {
                res = node.BuilderFetch(res);
                if (res == null)
                    throw new ArgumentException("Failed to follow path node " + node);
            }

            return res;
        }

        public bool TryFetch(JsonPath path, [MaybeNullWhen(returnValue: false)] out JsonBuilder result)
        {
            ArgumentNullException.ThrowIfNull(path);

            result = default!;
            JsonBuilder? data = this;
            foreach (var node in path.Nodes)
            {
                data = node.BuilderFetch(data);
                if (data == null)
                    return false;
            }

            result = data;
            return true;
        }

        public void Update(JsonPath path, ImmutableJson update)
        {
            Update(path, new JsonBuilder(update));
        }

        public void Update(JsonPath path, JsonBuilder update)
        {
            ArgumentNullException.ThrowIfNull(path);
            ArgumentNullException.ThrowIfNull(update);
            if (path.Head == null)
                throw new InvalidOperationException("JSON path is empty");

            var data = this;
            var nodes = path.Nodes;
            for (var i = 0; i < nodes.Length - 1; i++)
                data = nodes[i].BuilderFetch(data);
            nodes[nodes.Length - 1].BuilderUpdate(data, update);
        }

        public void ForceUpdate(JsonPath path, ImmutableJson update)
        {
            ForceUpdate(path, new JsonBuilder(update));
        }

        public void ForceUpdate(JsonPath path, JsonBuilder update)
        {
            ArgumentNullException.ThrowIfNull(path);
            ArgumentNullException.ThrowIfNull(update);
            if (path.Head == null)
                throw new InvalidOperationException("JSON path is empty");

            var data = this;
            var nodes = path.Nodes;
            for (var i = 0; i < nodes.Length; i++)
                data = nodes[i].BuilderEnsure(data);
            data.Set(update);
        }

        public void Delete(JsonPath path)
        {
            ArgumentNullException.ThrowIfNull(path);
            if (path.Head == null)
                throw new InvalidOperationException("JSON path is empty");

            var data = this;
            var nodes = path.Nodes;
            for (var i = 0; i < nodes.Length - 1; i++)
                data = nodes[i].BuilderFetch(data);

            nodes[nodes.Length - 1].BuilderDelete(data);
        }

        #endregion

        public ImmutableJson ToImmutable()
        {
            return data switch
            {
                ImmutableJson value => value,
                Dictionary<string, JsonBuilder> obj => new ImmutableJsonObject(obj.ToDictionary(pair => pair.Key, pair => pair.Value.ToImmutable())),
                List<JsonBuilder> arr => new ImmutableJsonArray(arr.Select(el => el.ToImmutable()).ToList()),
                _ => ImmutableJson.Null,
            };
        }

        public JsonBuilder Clone() => new JsonBuilder(ToImmutable());

        public override string ToString()
        {
            return ToImmutable().ToString();
        }

        #region Constructors

        public JsonBuilder()
        {
            data = null;
        }

        public JsonBuilder(Dictionary<string, JsonBuilder> value)
        {
            ArgumentNullException.ThrowIfNull(value);
            data = value;
        }

        public JsonBuilder(List<JsonBuilder> value)
        {
            ArgumentNullException.ThrowIfNull(value);
            data = value;
        }

        public JsonBuilder(ImmutableJson value)
        {
            ArgumentNullException.ThrowIfNull(value);
            data = value;
        }

        public static JsonBuilder Null => new JsonBuilder();

        public static JsonBuilder EmptyArray => new JsonBuilder(new List<JsonBuilder>());

        public static JsonBuilder EmptyObject => new JsonBuilder(new Dictionary<string, JsonBuilder>());

        #endregion
    }
}
