using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Json.Tests
{
    [TestFixture]
    public class JsonPathTests
    {
        IEnumerable<string> InvalidPaths()
        {
            return new List<string>
            {
                "no_closing_bracket[0",
                "no_opening_bracket]",
                ".starts_with_dot",
                "ends_with_dot.",
                "two..dots",
                "no_dot[0]after_index",
            };
        }

        public void AreEqual(ImmutableJson expected, ImmutableJson actual)
        {
            Assert.True(ImmutableJson.Equals(expected, actual));
            // TODO: doesn't work
            //Assert.AreEqual(expected, actual);
        }

        public void AreNotEqual(ImmutableJson expected, ImmutableJson actual)
        {
            Assert.True(expected != actual);
            Assert.AreNotEqual(expected, actual);
        }

        [Test]
        public void ParseInvalidPathFailTest()
        {
            foreach (var path in InvalidPaths())
                Assert.Throws<ArgumentException>(() => JsonPath.Parse(path));
        }

        [Test]
        public void ParseEmptyTest()
        {
            Assert.AreEqual(JsonPath.Empty, JsonPath.Parse(string.Empty));
        }

        [Test]
        public void ParseSimpleKeyTest()
        {
            Assert.AreEqual(JsonPath.Empty.AppendObject("test"), JsonPath.Parse("test"));
        }

        [Test]
        public void ParseDotsTest()
        {
            Assert.AreEqual(JsonPath.Empty.AppendObject("a").AppendObject("b").AppendObject("c"), JsonPath.Parse("a.b.c"));
        }

        [Test]
        public void ParseIndiciesTest()
        {
            Assert.AreEqual(JsonPath.Empty.AppendArray(0), JsonPath.Parse("[0]"));
            Assert.AreEqual(JsonPath.Empty.AppendObject("test").AppendArray(0), JsonPath.Parse("test[0]"));
            Assert.AreEqual(JsonPath.Empty.AppendObject("test").AppendArray(1).AppendArray(4), JsonPath.Parse("test[1][4]"));
            Assert.AreEqual(JsonPath.Empty.AppendObject("a").AppendArray(0).AppendObject("b"), JsonPath.Parse("a[0].b"));
        }

        [Test]
        public void JoinEmptyNotModifiedTest()
        {
            var path1 = JsonPath.Parse("test1");
            Assert.AreEqual(JsonPath.Empty, JsonPath.Join(JsonPath.Empty, JsonPath.Empty));
            Assert.AreEqual(JsonPath.Parse("test1"), JsonPath.Join(JsonPath.Empty, path1));
            Assert.AreEqual(JsonPath.Parse("test1"), JsonPath.Join(path1, JsonPath.Empty, JsonPath.Empty));
        }

        [Test]
        public void JoinComplexTest()
        {
            var path1 = JsonPath.Parse("test1");
            var path2 = JsonPath.Parse("test2[0]");
            var path3 = JsonPath.Parse("test3.test4");
            Assert.AreEqual("test1.test2[0].test3.test4", JsonPath.Join(path1, path2, path3).ToString());
            Assert.AreEqual("test1.test2[0].test3.test4", JsonPath.Join(path1, path2, path3).ToString());
            Assert.AreEqual("test3.test4", JsonPath.Join(path3).ToString());
        }

        public ImmutableJson MakeJson()
        {
            return new JsonObject
            {
                ["key11"] = new JsonObject
                    {
                        ["key21"] = new JsonObject
                        {
                            ["key31"] = 10,
                            ["key32"] = true
                        },
                        ["key22"] = new JsonObject
                        {
                            ["key31"] = "test",
                            ["key32"] = ImmutableJson.Null
                        }
                    },
                ["key12"] = new JsonObject
                    {
                        { "key21", new JsonArray { 1, "2", 3.1, true } },
                        { "key22", new JsonArray
                            {
                                new JsonObject
                                {
                                    ["arr11"] = "test",
                                    ["arr12"] = false
                                },
                                new JsonObject
                                {
                                    ["arr21"] = 1,
                                    ["arr22"] = 2.1
                                }
                            }
                        }
                    },
                ["key13"] = new JsonArray
                    {
                        new JsonArray { 1, 2, 3, 4 },
                        new JsonArray { true, false },
                    }
            };
        }

        public void CheckFetch(ImmutableJson json, string pathStr, ImmutableJson value)
        {
            var path = JsonPath.Parse(pathStr);
            AreEqual(value, json.Fetch(path));
            AreEqual(value, new JsonBuilder(json).Fetch(path).ToImmutable());
        }

        [Test]
        public void FetchTest()
        {
            var json = MakeJson();
            CheckFetch(json, "key11.key21.key31", 10);
            CheckFetch(json, "key11.key22.key32", ImmutableJson.Null);
            CheckFetch(json, "key12.key21[1]", "2");
            CheckFetch(json, "key12.key21[2]", 3.1);
            CheckFetch(json, "key12.key22[1].arr21", 1);
            CheckFetch(json, "key13[1][0]", true);
            CheckFetch(json, "key13[1]", new JsonArray() { true, false });
        }

        public void CheckPathUpdate(ImmutableJson json, string pathStr, ImmutableJson value)
        {
            var path = JsonPath.Parse(pathStr);
            var jsonChanged = json.Update(path, value);
            AreEqual(value, jsonChanged.Fetch(path));
            AreNotEqual(json, jsonChanged);
            var jsonSame = jsonChanged.Update(path, json.Fetch(path));
            AreEqual(json, jsonSame);

            // test builder
            var builder = new JsonBuilder(json);
            builder.Update(path, value);
            AreEqual(value, builder.Fetch(path).ToImmutable());
            builder.Update(path, json.Fetch(path));
            AreEqual(json, builder.ToImmutable());
        }

        [Test]
        public void UpdateTest()
        {
            var json = MakeJson();

            CheckPathUpdate(json, "key11.key21.key31", true);
            CheckPathUpdate(json, "key12.key21[1]", "somedata");
            CheckPathUpdate(json, "key13[1][1]", 5.5);
            CheckPathUpdate(json, "key11", new JsonArray() { 1, 2 });
        }

        [Test]
        public void ForceUpdateTest()
        {
            ImmutableJson pathJson = new JsonArray() { new JsonObject() { { "key", new JsonArray() { ImmutableJson.Null, "here" } } } };
            var data1 = ImmutableJson.Null.ForceUpdate(JsonPath.Parse("[0].key[1]"), "here");
            AreEqual(data1, pathJson);

            var data2 = ImmutableJson.True.ForceUpdate(JsonPath.Parse("key[1]"), "here");
            AreEqual(data2, pathJson[0]);
        }

        public void CheckPathDelete(ImmutableJson json, string pathStr)
        {
            ImmutableJson temp;
            var path = JsonPath.Parse(pathStr);

            Assert.True(json.TryFetch(path, out temp));
            AreEqual(temp, json.Fetch(path));

            var jsonChanged = json.Delete(path);
            Assert.False(jsonChanged.TryFetch(path, out temp));
            Assert.AreEqual(null, temp);

            var jsonSame = jsonChanged.ForceUpdate(path, json.Fetch(path));
            AreEqual(json, jsonSame);

            // test builder
            JsonBuilder tempBuilder;
            var builder = new JsonBuilder(json);

            Assert.True(builder.TryFetch(path, out tempBuilder));
            AreEqual(tempBuilder.ToImmutable(), builder.Fetch(path).ToImmutable());

            builder.Delete(path);
            Assert.False(builder.TryFetch(path, out tempBuilder));
            Assert.AreEqual(null, tempBuilder);

            builder.ForceUpdate(path, json.Fetch(path));
            AreEqual(json, builder.ToImmutable());
        }

        [Test]
        public void DeleteTest()
        {
            var json = MakeJson();

            CheckPathDelete(json, "key11.key21");
            CheckPathDelete(json, "key12.key21");
            CheckPathDelete(json, "key11");
            // tail array tests
            CheckPathDelete(json, "key13[1]");
            CheckPathDelete(new JsonArray() { true, false }, "[1]");
        }

        [Test]
        public void UpdateObjectKeyTest()
        {
            var innerJson = new JsonObject
            {
                ["key_inner"] = 10,
                ["value"] = true
            };

            var baseJson = new JsonObject
            {
                ["key"] = innerJson,
                ["other"] = "hello"
            };

            var jsonOriginal = baseJson.ToImmutable();

            innerJson["key_inner_new"] = innerJson["key_inner"];
            innerJson.Remove("key_inner");
            baseJson["key"] = innerJson;

            var jsonExpected = baseJson.ToImmutable();

            var path = new JsonPath().AppendObject("key").AppendObjectKey("key_inner");

            // json test
            var jsonChanged = jsonOriginal.Update(path, "key_inner_new");
            AreEqual(jsonExpected, jsonChanged);

            // builder test
            var builder = new JsonBuilder(jsonOriginal);
            builder.Update(path, "key_inner_new");
            AreEqual(jsonExpected, builder.ToImmutable());
        }
    }
}
