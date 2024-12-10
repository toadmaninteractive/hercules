using Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JsonDiff
{
    public class JsonDiffChangesFormatter
    {
        static readonly JsonFormat AddedFormat = new JsonFormat(true, "    ", "+  ");
        static readonly JsonFormat RemovedFormat = new JsonFormat(true, "    ", "-  ");

        public bool HasChanges { get; private set; }
        public StringBuilder Builder { get; private set; }
        public List<DiffLineStyle> Lines { get; private set; }

        public JsonDiffChangesFormatter()
        {
            this.Builder = new StringBuilder();
            this.Lines = new List<DiffLineStyle>();
        }

        void WriteJson(DiffLineStyle mode, JsonFormat format, ImmutableJson json)
        {
            var formattedJson = json.ToString(format);
            Builder.AppendLine(formattedJson);
            var lines = formattedJson.Count(c => c == '\n') + 1;
            for (int i = 0; i < lines; i++)
                Lines.Add(mode);
        }

        void WriteLine(string line)
        {
            Builder.AppendLine(line);
            Lines.Add(DiffLineStyle.Normal);
        }

        void SkipLine()
        {
            Builder.AppendLine();
            Lines.Add(DiffLineStyle.Skip);
        }

        public void Process(JsonDiff diff, Action<JsonDiffChunk, int>? chunkLineCallback = null)
        {
            var chunks = diff.GetChunks();
            var shouldSkip = false;
            foreach (var chunk in chunks)
            {
                if (shouldSkip)
                    SkipLine();
                shouldSkip = true;

                chunkLineCallback?.Invoke(chunk, Lines.Count);
                WriteLine(chunk.Path.ToString());
                if (chunk.Left != null)
                    WriteJson(DiffLineStyle.Deleted, RemovedFormat, chunk.Left);
                if (chunk.Right != null)
                    WriteJson(DiffLineStyle.Added, AddedFormat, chunk.Right);
            }
            HasChanges = chunks.Any();
        }
    }
}
