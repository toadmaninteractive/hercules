using Hercules.Forms.Elements;
using Hercules.Forms.Schema;
using System;
using System.Windows;

namespace Hercules.Diagrams
{
    public class Archetype
    {
        public ArchetypeType Type { get; }

        public Size BlockSize { get; }

        public string? DefaultIconName { get; }

        public BlockBase CreateBlock(SchemaBlock prototype, BlockListItem element) => blockFactory(prototype, element);

        private readonly Func<SchemaBlock, BlockListItem, BlockBase> blockFactory;

        public Archetype(ArchetypeType type, Size size, string? defaultIconName, Func<SchemaBlock, BlockListItem, BlockBase> blockFactory)
        {
            Type = type;
            BlockSize = size;
            DefaultIconName = defaultIconName;

            this.blockFactory = blockFactory;
        }
    }
}