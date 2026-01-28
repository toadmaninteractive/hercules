using Hercules.Scripting.JavaScript;
using Jint.Native;
using Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;

namespace Hercules.Scripting
{
    public class HerculesXmlApi
    {
        public static readonly CompletionData[] Completion = ScriptingApiHelper.GetCompletionData(typeof(HerculesXmlApi));

        public Dictionary<string, object> Api => ScriptingApiHelper.GetApi(this);

        public ScriptContext Context { get; }
        public JsHost Host { get; }

        public HerculesXmlApi(ScriptContext context, JsHost host)
        {
            Context = context;
            Host = host;
        }

        [ScriptingApi("toJson", "Convert xml string to JSON.",
            Example = "hercules.xml.toJson(xmlString);")]
        public JsValue ToJson(string xmlString)
        {
            var xml = new XmlDocument();
            xml.LoadXml(xmlString);
            var jsonBuilder = XmlNodeToJson(xml.ChildNodes!.Cast<XmlNode>().FirstOrDefault(n => n is not XmlDeclaration)!);
            var json = jsonBuilder.ToImmutable();
            return Host.JsonToJsValue(json);
        }

        public record XmlFromJsonOptions(IReadOnlyList<string> attributes);

        [ScriptingApi("fromJson", "Convert JSON object to xml string.",
            Example = "hercules.xml.fromJson(json, rootName);")]
        public string FromJson(JsValue json, string root, JsValue? options)
        {
            var immutableJson = Host.JsValueToJson(json);
            ImmutableJsonObject jsonOptions = options == null ? ImmutableJsonObject.Empty : Host.JsValueToJson(options).AsObject;
            var attributes = jsonOptions.GetValueOrDefault("attributes", ImmutableJson.EmptyArray).AsArray.Select(i => i.AsString).ToList();
            var xmlFromJsonOptions = new XmlFromJsonOptions(attributes);
            var ns = jsonOptions.GetValueOrDefault("xmlns")?.AsString;
            var indent = jsonOptions.GetValueOrDefault("indent") ?? "  ";
            var encodingString = jsonOptions.GetValueOrDefault("encoding")?.AsString;
            var encoding = encodingString == null ? Encoding.UTF8 : Encoding.GetEncoding(encodingString);
            XmlWriterSettings writerSettings = new XmlWriterSettings
            {
                NewLineChars = Environment.NewLine,
                Indent = !indent.IsNull,
                IndentChars = indent.IsString ? indent.AsString : string.Empty,
                Encoding = encoding,
                CloseOutput = true
            };

            var sw = new StringWriterWithEncoding(encoding);
            using XmlWriter xml = XmlWriter.Create(sw, writerSettings);
            xml.WriteStartDocument();
            WriteNode(xml, immutableJson, root, xmlFromJsonOptions, ns);
            xml.WriteEndDocument();
            xml.Flush();
            return sw.ToString();
        }

        private static string FormatAttribute(ImmutableJson value, string key, XmlFromJsonOptions options) => value switch
        {
            ImmutableJsonBoolean b => b.Value ? "1" : "0",
            ImmutableJsonInteger i => i.Value.ToString(CultureInfo.InvariantCulture),
            ImmutableJsonDouble d => d.Value.ToString(CultureInfo.InvariantCulture),
            ImmutableJsonLong l => l.Value.ToString(CultureInfo.InvariantCulture),
            ImmutableJsonString s => s.Value,
            ImmutableJson other => throw new InvalidOperationException($"Unsupported attribute value {key}={other.ToString(JsonFormat.Compact)}")
        };

        private static readonly char[] SpecialChars = ['<', '>', '&', '\n', '\r'];

        private static void WriteNode(XmlWriter xml, ImmutableJson json, string name, XmlFromJsonOptions options, string? ns = null)
        {
            switch (json)
            {
                case ImmutableJsonObject jsonObject:
                    xml.WriteStartElement(name, ns);
                    foreach (var attr in jsonObject)
                    {
                        if (!options.attributes.Contains(attr.Key))
                            continue;
                        var attrValue = FormatAttribute(attr.Value, attr.Key, options);
                        xml.WriteAttributeString(attr.Key, attrValue);
                    }
                    foreach (var pair in jsonObject)
                    {
                        if (options.attributes.Contains(pair.Key))
                            continue;
                        WriteNode(xml, pair.Value, pair.Key, options);
                    }
                    xml.WriteEndElement();
                    break;
                case ImmutableJsonArray arr:
                    foreach (var item in arr)
                    {
                        WriteNode(xml, item, name, options);
                    }
                    break;
                case ImmutableJsonString jsonString:
                    {
                        var value = jsonString.Value;
                        if (value.IndexOfAny(SpecialChars) != -1)
                        {
                            xml.WriteStartElement(name);
                            xml.WriteCData(value);
                            xml.WriteEndElement();
                        }
                        else
                        {
                            xml.WriteElementString(name, value);
                        }
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported element {json.ToString(JsonFormat.Compact)}");
            }
        }

        private static JsonBuilder XmlNodeToJson(XmlNode node)
        {
            var hasAttributes = node.Attributes is { Count: > 0 };
            if (node.HasChildNodes || hasAttributes)
            {
                if (node.HasChildNodes && node.ChildNodes.Count == 1 && !hasAttributes && node.ChildNodes[0] is { Value: not null })
                {
                    return new JsonBuilder(ImmutableJson.Create(node.ChildNodes[0]!.Value!.Replace("\r", "", StringComparison.Ordinal)));
                }

                var json = JsonBuilder.EmptyObject;
                if (hasAttributes)
                {
                    foreach (XmlAttribute attr in node.Attributes!)
                    {
                        json.AsObject.Add(attr.Name, new JsonBuilder(attr.Value));
                    }
                }

                if (node.HasChildNodes)
                {
                    foreach (XmlNode child in node.ChildNodes)
                    {
                        var nodeAsJson = XmlNodeToJson(child);
                        if (json.AsObject.TryGetValue(child.Name, out var valueOrArray))
                        {
                            if (valueOrArray.IsArray)
                            {
                                valueOrArray.AsArray.Add(nodeAsJson);
                            }
                            else
                            {
                                var arrayBuilder = JsonBuilder.EmptyArray;
                                arrayBuilder.AsArray.Add(valueOrArray.Clone());
                                arrayBuilder.AsArray.Add(nodeAsJson);
                                json.AsObject[child.Name].Set(arrayBuilder);
                            }
                        }
                        else
                            json.AsObject.Add(child.Name, nodeAsJson);
                    }
                }

                return json;
            }
            else if (node.Value != null)
            {
                return new JsonBuilder(ImmutableJson.Create(node.Value));
            }
            else
                return JsonBuilder.Null;
        }
    }
}
