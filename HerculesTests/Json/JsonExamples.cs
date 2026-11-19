using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Json.Tests
{
    public static class JsonExamples
    {
        public static readonly string JsonExampleString1 =
@"{
    ""glossary"": {
        ""title"": ""example glossary"",
        ""GlossDiv"": {
            ""title"": ""S"",
            ""GlossList"": {
                ""GlossEntry"": {
                    ""ID"": ""SGML"",
                    ""SortAs"": ""SGML"",
                    ""GlossTerm"": ""Standard Generalized Markup Language"",
                    ""Acronym"": ""SGML"",
                    ""Abbrev"": ""ISO 8879:1986"",
                    ""GlossDef"": {
                        ""para"": ""A meta-markup language, used to create markup languages such as DocBook."",
                        ""GlossSeeAlso"": [""GML"", ""XML""]
                    },
                    ""GlossSee"": ""markup""
                }
            }
        }
    }
}";

        public static readonly ImmutableJson JsonExample1 =
            new JsonObject
            {
                ["glossary"] = new JsonObject
                {
                    ["title"] = "example glossary",
                    ["GlossDiv"] = new JsonObject
                    {
                        ["title"] = "S",
                        ["GlossList"] = new JsonObject
                        {
                            ["GlossEntry"] = new JsonObject
                            {
                                ["ID"] = "SGML",
                                ["SortAs"] = "SGML",
                                ["GlossTerm"] = "Standard Generalized Markup Language",
                                ["Acronym"] = "SGML",
                                ["Abbrev"] = "ISO 8879:1986",
                                ["GlossDef"] = new JsonObject
                                {
                                    ["para"] = "A meta-markup language, used to create markup languages such as DocBook.",
                                    ["GlossSeeAlso"] = new JsonArray { "GML", "XML" }
                                },
                                ["GlossSee"] = "markup"
                            }
                        }
                    }
                }
            };

        public static string JsonExampleString2 =
@"{""menu"": {
        ""id"": ""file"",
        ""value"": ""File"",
        ""popup"": {
            ""menuitem"": [
                {""value"": ""New"", ""onclick"": ""CreateNewDoc()""},
                {""value"": ""Open"", ""onclick"": ""OpenDoc()""},
                {""value"": ""Close"", ""onclick"": ""CloseDoc()""}
            ]
        }
}}";

        public static ImmutableJson JsonExample2 = new JsonObject
        {
            ["menu"] = new JsonObject
            {
                ["id"] = "file",
                ["value"] = "File",
                ["popup"] = new JsonObject
                {
                    ["menuitem"] = new JsonArray {
                        new JsonObject { ["value"] = "New", ["onclick"] = "CreateNewDoc()" },
                        new JsonObject { ["value"] = "Open", ["onclick"] = "OpenDoc()" },
                        new JsonObject { ["value"] = "Close", ["onclick"] = "CloseDoc()" }
                    }
                }
            }
        };

        public static string JsonExampleString3 =
@"{""widget"": {
    ""debug"": ""on"",
    ""window"": {
        ""title"": ""Sample Konfabulator Widget"",
        ""name"": ""main_window"",
        ""width"": 500,
        ""height"": 500
    },
    ""image"": {
        ""src"": ""Images/Sun.png"",
        ""name"": ""sun1"",
        ""hOffset"": 250,
        ""vOffset"": 250,
        ""alignment"": ""center""
    },
    ""text"": {
        ""data"": ""Click Here"",
        ""size"": 36,
        ""style"": ""bold"",
        ""name"": ""text1"",
        ""hOffset"": 250,
        ""vOffset"": 100,
        ""alignment"": ""center"",
        ""onMouseUp"": ""sun1.opacity = (sun1.opacity / 100) * 90;""
    }
}}";

        public static ImmutableJson JsonExample3 = new JsonObject
        {
            ["widget"] = new JsonObject
            {
                ["debug"] = "on",
                ["window"] = new JsonObject
                {
                    ["title"] = "Sample Konfabulator Widget",
                    ["name"] = "main_window",
                    ["width"] = 500,
                    ["height"] = 500
                },
                ["image"] = new JsonObject
                {
                    ["src"] = "Images/Sun.png",
                    ["name"] = "sun1",
                    ["hOffset"] = 250,
                    ["vOffset"] = 250,
                    ["alignment"] = "center"
                },
                ["text"] = new JsonObject
                {
                    ["data"] = "Click Here",
                    ["size"] = 36,
                    ["style"] = "bold",
                    ["name"] = "text1",
                    ["hOffset"] = 250,
                    ["vOffset"] = 100,
                    ["alignment"] = "center",
                    ["onMouseUp"] = "sun1.opacity = (sun1.opacity / 100) * 90;"
                }
            }
        };
    }
}
