using Hercules.AI;
using JqSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Hercules.Scripting.JavaScript
{
    /// <summary>
    /// Runs a jq query over JSON text, with limits on time, memory and output size.
    /// env, modules and input are not available to the query.
    /// </summary>
    public static class JqRunner
    {
        public const int MaxResults = 1000;
        public const int MaxOutputChars = 200_000;
        private const int MemoryCheckMilliseconds = 50;

        /// <returns>One compact JSON value per line.</returns>
        public static string Run(string query, string inputJson, TimeSpan timeout, long maxMemoryBytes)
        {
            var (lines, truncated) = RunLines(query, inputJson, timeout, maxMemoryBytes);
            var count = lines.Count;
            if (count == 0 && truncated)
                throw new AiToolException($"The first result is bigger than {MaxOutputChars} characters. Make the query more selective.");
            if (count == 0)
                return "[No results]";
            var output = new StringBuilder();
            foreach (var line in lines)
                output.AppendLine(line);
            if (truncated)
                output.AppendLine($"[Output truncated after {count} results. Make the query more selective, e.g. with limit(100; ...).]");
            return output.ToString();
        }

        /// <returns>Compact JSON values; <c>truncated</c> is set if the result caps cut the output.</returns>
        public static (List<string> Lines, bool Truncated) RunLines(string query, string inputJson, TimeSpan timeout, long maxMemoryBytes)
        {
            JqCode code;
            try
            {
                code = JqQuery.Parse(query).Compile();
            }
            catch (JqException exception)
            {
                throw new AiToolException($"Invalid jq query: {exception.Message}");
            }
            var input = JqValue.FromJson(inputJson);

            // A runaway query (e.g. a recursion without a base case) can allocate gigabytes within seconds,
            // so a timeout alone is not enough.
            using var timeoutSource = new CancellationTokenSource(timeout);
            using var memorySource = new CancellationTokenSource();
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, memorySource.Token);
            var baseline = GC.GetTotalMemory(false);
            using var watchdog = new Timer(_ => CancelIfOverLimit(memorySource, baseline, maxMemoryBytes), null, MemoryCheckMilliseconds, MemoryCheckMilliseconds);

            var lines = new List<string>();
            var length = 0;
            var truncated = false;
            try
            {
                foreach (var value in code.Run(input, linkedSource.Token))
                {
                    var line = JqValue.ToJson(value);
                    if (lines.Count >= MaxResults || length + line.Length > MaxOutputChars)
                    {
                        truncated = true;
                        break;
                    }
                    lines.Add(line);
                    length += line.Length + Environment.NewLine.Length;
                }
            }
            catch (OperationCanceledException)
            {
                throw new AiToolException(memorySource.IsCancellationRequested
                    ? $"The query was stopped: it used more than {maxMemoryBytes / 1024 / 1024} MB of memory. Make it more selective."
                    : $"The query was stopped: it ran longer than {timeout.TotalSeconds:0.#} s. Make it more selective.");
            }
            catch (JqException exception)
            {
                throw new AiToolException($"jq error: {exception.Message}");
            }

            return (lines, truncated);
        }

        private static void CancelIfOverLimit(CancellationTokenSource source, long baseline, long maxMemoryBytes)
        {
            if (GC.GetTotalMemory(false) - baseline <= maxMemoryBytes)
                return;
            // The first measure may include garbage that is not collected yet.
            if (GC.GetTotalMemory(true) - baseline > maxMemoryBytes)
                source.Cancel();
        }
    }
}
