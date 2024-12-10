using Hercules.Forms.Schema;
using Jint;
using Jint.Native;
using Jint.Runtime;
using Json;
using System;
using System.Globalization;
using System.Linq;

namespace Hercules.Scripting.JavaScript
{
    public class ImmutableJsonDateTime : ImmutableJson
    {
        public ImmutableJsonDateTime(DateTime value)
        {
            Value = value;
        }

        public DateTime Value { get; }

        public override bool Equals(ImmutableJson? other)
        {
            return other is ImmutableJsonDateTime dateTime && dateTime.Value == Value;
        }

        public override JsonType Type => JsonType.String;
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString(DateTimeSchemaType.Culture);
        }
    }

    public static class JsValueConverter
    {
        public static JsValue FromJsonValue(ImmutableJson? json, Engine engine)
        {
            if (json == null)
                return JsValue.Undefined;
            if (json.IsNull)
                return JsValue.Null;
            if (json.IsBool)
                return json.AsBool ? JsBoolean.True : JsBoolean.False;
            if (json is ImmutableJsonDateTime dateTimeSchemaType)
            {
                return new JsDate(engine, dateTimeSchemaType.Value);
            }
            if (json.IsString)
                return new JsString(json.AsString);
            if (json.IsNumber)
                return new JsNumber(json.AsNumber);
            if (json.IsArray)
            {
                var jsArray = engine.Realm.Intrinsics.Array.Construct(Arguments.Empty);
                engine.Realm.Intrinsics.Array.PrototypeObject.Push(jsArray, json.AsArray.Select(i => FromJsonValue(i, engine)).ToArray());
                return jsArray;
            }
            if (json.IsObject)
            {
                var jsObject = engine.Realm.Intrinsics.Object.Construct(Arguments.Empty);
                foreach (var pair in json.AsObject)
                {
                    var value = FromJsonValue(pair.Value, engine);
                    jsObject.Set(pair.Key, value, true);
                }
                return jsObject;
            }
            return JsValue.Undefined;
        }

        public static ImmutableJson ToJsonValue(JsValue val, bool supportDateTime = false)
        {
            if (val.IsUndefined())
                return ImmutableJson.Null;
            if (val.IsBoolean())
                return val.AsBoolean();
            if (val.IsString())
            {
                return val.AsString();
                // return val.AsString().Replace("\r", "", System.StringComparison.Ordinal);
            }
            if (val.IsNumber())
            {
                var res = val.AsNumber();
                if (res == (int)res)
                    return (int)res;
                return res;
            }
            if (val.IsNull())
                return ImmutableJson.Null;
            if (val.IsDate())
            {
                return supportDateTime ? new ImmutableJsonDateTime(val.AsDate().ToDateTime()) : ImmutableJson.Create(val.AsDate().ToDateTime().ToString(DateTimeSchemaType.Culture));
            }
            if (val.IsArray())
            {
                var inst = val.AsArray();
                var len = TypeConverter.ToInt32(inst.Get("length"));
                var result = new JsonArray(len);
                for (var k = 0; k < len; k++)
                {
                    var pk = k.ToString(CultureInfo.InvariantCulture);
                    var kpresent = inst.HasProperty(pk);
                    if (kpresent)
                    {
                        var kvalue = inst.Get(pk);
                        result.Add(ToJsonValue(kvalue));
                    }
                    else
                    {
                        result.Add(ImmutableJson.Null);
                    }
                }
                return result;
            }
            if (val.IsObject())
            {
                var inst = val.AsObject();
                var pairs = inst.GetOwnProperties().Where(p => p.Value.Enumerable);
                var result = new JsonObject();
                foreach (var pair in pairs)
                {
                    var jsValue = inst.Get(pair.Key);
                    if (!jsValue.IsUndefined())
                    {
                        var jsonValue = ToJsonValue(jsValue);
                        result.Add(pair.Key.AsString(), jsonValue);
                    }
                }
                return result;
            }
            return ImmutableJson.Null;
        }
    }
}
