using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;

namespace JsonDiff
{
    public interface IDiffChunk
    {
        int Line { get; }
        bool Selected { get; set; }
    }

    public interface IDiffChunks
    {
        IDiffChunk? GetLineChunk(int lineIndex, bool headerOnly);

        IObservable<Unit> WhenChanged { get; }
    }

    public class DiffChunks : IDiffChunks
    {
        public IReadOnlyCollection<IDiffChunk> Chunks { get; }
        public IObservable<Unit> WhenChanged { get; }

        public DiffChunks(IReadOnlyCollection<IDiffChunk> chunks, IObservable<Unit> whenChanged)
        {
            this.WhenChanged = whenChanged;
            this.Chunks = chunks;
        }

        public IDiffChunk? GetLineChunk(int lineIndex, bool headerOnly)
        {
            if (!headerOnly)
                return Chunks.TakeWhile(c => c.Line <= lineIndex).LastOrDefault();
            foreach (var chunk in Chunks)
            {
                if (chunk.Line == lineIndex)
                    return chunk;
                if (chunk.Line > lineIndex)
                    return null;
            }
            return null;
        }
    }
}
