using NUnit.Framework;
using System;
using System.Linq;

namespace Hercules.AI.Tests
{
    [TestFixture]
    public class JqRunnerTests
    {
        private const string Documents = """
            [
              {"_id": "a", "category": "resource", "mass": 5},
              {"_id": "b", "category": "resource", "mass": 20},
              {"_id": "c", "category": "item", "effect": {"triggers": [{"impact": {"type": "target_impact", "target": "event", "impact": {"type": "hit"}}}]}}
            ]
            """;

        private static string Run(string query, string input = Documents, double timeoutSeconds = 10, long maxMemoryMegabytes = 512)
        {
            return JqRunner.Run(query, input, TimeSpan.FromSeconds(timeoutSeconds), maxMemoryMegabytes * 1024 * 1024);
        }

        private static string[] Lines(string output)
        {
            return output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        }

        [Test]
        public void Filter_ReturnsOneJsonValuePerLine()
        {
            var output = Run(".[] | select(.mass > 10) | {_id, mass}");
            Assert.That(Lines(output), Is.EqualTo(new[] { "{\"_id\":\"b\",\"mass\":20}" }));
        }

        [Test]
        public void RecursiveSearch_FindsNestedRecords()
        {
            var output = Run(".[] | ._id as $id | .. | objects | select(.type? == \"target_impact\") | {doc: $id, target, inner: .impact.type}");
            Assert.That(Lines(output), Is.EqualTo(new[] { "{\"doc\":\"c\",\"inner\":\"hit\",\"target\":\"event\"}" }));
        }

        [Test]
        public void PathOfRecursiveDescent_Works()
        {
            var output = Run(".[2] | [path(.. | select(type == \"object\" and .type == \"target_impact\")) | map(tostring) | join(\".\")] | .[]");
            Assert.That(Lines(output), Is.EqualTo(new[] { "\"effect.triggers.0.impact\"" }));
        }

        [Test]
        public void GroupBy_Aggregates()
        {
            var output = Run("group_by(.category) | map({category: .[0].category, count: length})");
            Assert.That(output.Trim(), Is.EqualTo("[{\"category\":\"item\",\"count\":1},{\"category\":\"resource\",\"count\":2}]"));
        }

        [Test]
        public void NoResults_ReportsIt()
        {
            Assert.That(Run(".[] | select(false)"), Is.EqualTo("[No results]"));
        }

        [Test]
        public void InvalidQuery_ThrowsToolException()
        {
            Assert.That(() => Run(".[ | "), Throws.TypeOf<AiToolException>().With.Message.Contains("Invalid jq query"));
        }

        [Test]
        public void RuntimeError_ThrowsToolException()
        {
            Assert.That(() => Run("error(\"boom\")"), Throws.TypeOf<AiToolException>().With.Message.Contains("boom"));
        }

        [Test]
        public void Environment_IsNotVisible()
        {
            Assert.That(Run("env | length").Trim(), Is.EqualTo("0"));
            Assert.That(Run("$ENV.PATH").Trim(), Is.EqualTo("null"));
        }

        [Test]
        public void ModulesAndInput_AreNotAvailable()
        {
            Assert.That(() => Run("import \"x\" as x; 1"), Throws.TypeOf<AiToolException>());
            Assert.That(() => Run("input"), Throws.TypeOf<AiToolException>());
        }

        [Test]
        public void RunawayLoop_IsStoppedByTimeout()
        {
            Assert.That(() => Run("last(range(1e12))", timeoutSeconds: 0.5), Throws.TypeOf<AiToolException>().With.Message.Contains("ran longer"));
        }

        [Test]
        public void RunawayRecursion_IsStoppedByMemoryLimit()
        {
            Assert.That(() => Run("def f: 1 + f; f", timeoutSeconds: 30, maxMemoryMegabytes: 64), Throws.TypeOf<AiToolException>().With.Message.Contains("memory"));
        }

        [Test]
        public void TooManyResults_AreTruncated()
        {
            var lines = Lines(Run("range(2000)"));
            Assert.That(lines, Has.Length.EqualTo(JqRunner.MaxResults + 1));
            Assert.That(lines.Last(), Does.StartWith("[Output truncated after 1000 results"));
        }

        [Test]
        public void HugeFirstResult_ThrowsToolException()
        {
            Assert.That(() => Run("\"x\" * 300000"), Throws.TypeOf<AiToolException>().With.Message.Contains("bigger"));
        }
    }
}
