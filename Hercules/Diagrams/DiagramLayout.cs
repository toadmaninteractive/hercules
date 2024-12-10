using Hercules.Forms.Elements;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Hercules.Diagrams
{
    public class DiagramLayout
    {
        private class IncomingLinksComparer : IComparer<BlockBase>
        {
            private readonly DiagramViewModel diagram;

            public int Compare(BlockBase? x, BlockBase? y)
            {
                if (x is null && y is null)
                    return 0;
                if (x is null)
                    return -1;
                if (y is null)
                    return 1;
                var xIncomingLinks = diagram.InternalLinks.Where(l => l.Target == x);
                var yIncomingLinks = diagram.InternalLinks.Where(l => l.Target == y);
                if (!xIncomingLinks.Any() || !yIncomingLinks.Any())
                    return 0;

                var minPositionX = xIncomingLinks.Select(l => l.Source.Position.X).Min();
                var minPositionY = yIncomingLinks.Select(l => l.Source.Position.X).Min();

                if (minPositionX > minPositionY)
                    return 1;
                else if (minPositionY > minPositionX)
                    return -1;

                return 0;
            }

            public IncomingLinksComparer(DiagramViewModel diagram)
            {
                this.diagram = diagram;
            }
        }

        private class Layer
        {
            public double Width { get; set; }
            public double Height { get; set; }
            public List<BlockBase> Blocks { get; }

            public Layer()
            {
                Blocks = new List<BlockBase>();
            }
        }

        public void AutoLayout(DiagramViewModel diagram, ITransaction transaction)
        {
            if (diagram.InternalItems.Count == 0)
                return;

            var assetBlocks =
                diagram.InternalItems
                .Where(x => x.Connectors.Any(c =>
                    {
                        if (c.Offset.X > 0.5)
                            return false;

                        if (c.Offset.Y < 0.5)
                            return c.Offset.X < c.Offset.Y;
                        else
                            return c.Offset.X < 1 - c.Offset.Y;
                    }))
                .ToList();

            var blockWithOutAssets = diagram.InternalItems.Where(x => x.Type != ArchetypeType.Asset).ToList();
            var startBlocks = blockWithOutAssets.Except(diagram.InternalLinks.Select(x => x.Target)).ToList();

            IncomingLinksComparer incomingLinksComparer = new IncomingLinksComparer(diagram);
            double sumMaxLayersWidth = 0;
            double absoluteLayersHeight = 0;
            foreach (var startBlock in startBlocks.OrderBy(x => x.Position.X))
            {
                var layers = new Dictionary<int, Layer>();
                MakeLayers(diagram, startBlock, layers, 0, startBlocks);
                var layersHeight = layers.Sum(x => x.Value.Height + 40);
                var maxLayersWidth = layers.Max(x => x.Value.Width + x.Value.Blocks.Count * 40);
                var rootPosition = new Point(sumMaxLayersWidth + maxLayersWidth / 2, 0);
                sumMaxLayersWidth += maxLayersWidth;

                if (absoluteLayersHeight < layersHeight)
                    absoluteLayersHeight = layersHeight;

                var topPoint = startBlock.Height + 40;
                foreach (KeyValuePair<int, Layer> layer in layers)
                {
                    var leftPoint = rootPosition.X - layer.Value.Width / 2;

                    foreach (var blockView in layer.Value.Blocks.OrderBy(x => x, incomingLinksComparer))
                    {
                        blockView.SetPosition(new Point(leftPoint, topPoint), transaction);
                        leftPoint += blockView.Width + 40;
                    }

                    topPoint += layer.Value.Height + 40;
                }
            }

            if (assetBlocks.Count > 0)
            {
                var assetTopPoint = absoluteLayersHeight / 2 - assetBlocks.First().Height / 2;
                foreach (var assetBlockView in assetBlocks)
                {
                    if (!assetBlockView.FormListItem.WasModified(transaction))
                    {
                        assetBlockView.SetPosition(new Point(sumMaxLayersWidth + 40, assetTopPoint), transaction);
                        assetTopPoint += assetBlockView.Height + 40;
                    }
                }
            }
        }

        private void MakeLayers(DiagramViewModel diagram, BlockBase block, Dictionary<int, Layer> layers, int previousLayer, IReadOnlyList<BlockBase> assetBlocks)
        {
            int currentLayer = previousLayer + 1;
            if (layers.Keys.Contains(currentLayer))
            {
                if (layers[currentLayer].Blocks.Contains(block))
                    return;

                layers[currentLayer].Blocks.Add(block);
                layers[currentLayer].Width += block.Width + 40;

                if (layers[currentLayer].Height < block.Height)
                    layers[currentLayer].Height = block.Height;
            }
            else
            {
                var layer = new Layer();
                layer.Blocks.Add(block);
                layer.Width = block.Width;
                layer.Height = block.Height;
                layers[currentLayer] = layer;
            }

            foreach (var nextBlokView in
                diagram.InternalLinks
                    .Where(x => x.Source == block && !assetBlocks.Contains(x.Target))
                    .Select(x => x.Target)
                    .OrderBy(x => x.Position.X))
            {
                if (!layers.SelectMany(x => x.Value.Blocks).Contains(nextBlokView))
                    MakeLayers(diagram, nextBlokView, layers, currentLayer, assetBlocks);
            }
        }
    }
}