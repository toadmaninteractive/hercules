using Hercules.Documents.Editor;
using Hercules.Forms.Elements;
using Hercules.Forms.Schema;
using Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Telerik.Windows.Controls.Diagrams.Extensions.ViewModels;

namespace Hercules.Diagrams
{
    /// <summary>
    /// ViewModel for Diagram. Binding to GraphSource property.
    /// </summary>
    public class DiagramViewModel : ObservableGraphSourceBase<BlockBase, Link>
    {
        public ElementProperties Properties { get; }
        private readonly DocumentForm form;
        private readonly SchemaDiagram schemaDiagram;

        private readonly IReadOnlyList<ListElement> blockLists;
        private readonly ListElement? linkList;

        //For transactions
        private BlockBase? currentSelected;

        public ItemViewModelBase? CurrentSelected
        {
            get => currentSelected;
            set
            {
                if (value is BlockBase newValue)
                {
                    if (newValue.FormListItem != null)
                        currentSelected = newValue;

                    Properties.SetFields(currentSelected?.FormListItem?.StructElement.Children ?? form.Root.Record.Children);
                }
                else if (value is Link)
                    Properties.SetFields(null);
                else
                    Properties.SetFields(form.Root.Record.Children);
            }
        }

        public DiagramViewModel(DocumentForm form, SchemaDiagram schemaDiagram, ElementProperties properties)
        {
            this.form = form;
            this.schemaDiagram = schemaDiagram;
            this.Properties = properties;
            blockLists = form.GetBlockLists();
            linkList = form.GetLinkList();
            LoadContent();
            form.OnTransactionFinished += Form_OnTransactionFinished;
            CurrentSelected = null;
        }

        // preallocated lists used only in Form_OnTransactionFinished
        private readonly List<BlockBase> addedBlocks = new List<BlockBase>();
        private readonly List<BlockBase> modifiedBlocks = new List<BlockBase>();
        private readonly List<BlockBase> blocksWithPrototypeChanged = new List<BlockBase>();
        private readonly List<BlockBase> removedBlocks = new List<BlockBase>();
        private readonly HashSet<LinkListItem> addedLinks = new HashSet<LinkListItem>();
        private readonly HashSet<Link> removedLinks = new HashSet<Link>();

        private void Form_OnTransactionFinished(ITransaction transaction)
        {
            try
            {
                foreach (var blockList in blockLists)
                {
                    if (blockList.WasModified(transaction))
                    {
                        foreach (var listItem in blockList.Children)
                        {
                            if (listItem.WasModified(transaction))
                            {
                                var blockListItem = (BlockListItem)listItem;
                                var existingBlock = this.InternalItems.FirstOrDefault(x => x.FormListItem == listItem);
                                if (existingBlock == null)
                                {
                                    var block = AddBlockToDiagram(blockListItem);
                                    addedBlocks.Add(block);
                                }
                                else
                                {
                                    var newPrototype = blockListItem.Prototype ?? SchemaBlock.InvalidBlock;
                                    if (existingBlock.Prototype != newPrototype)
                                    {
                                        var newBlock = AddBlockToDiagram(blockListItem);
                                        addedBlocks.Add(newBlock);
                                        blocksWithPrototypeChanged.Add(existingBlock);
                                    }
                                    else
                                        modifiedBlocks.Add(existingBlock);
                                }
                            }
                        }
                    }
                }

                foreach (var block in addedBlocks)
                {
                    RestorePropertyLinks(block);
                    block.UpdateConnectorFillColors();
                }

                if (linkList != null && linkList.WasModified(transaction))
                {
                    foreach (var linkItem in linkList.Children.Cast<LinkListItem>())
                    {
                        if (linkItem.WasModified(transaction))
                        {
                            var link = InternalLinks.FirstOrDefault(l => l.FormListItem == linkItem);
                            if (link != null)
                                removedLinks.Add(link);
                            addedLinks.Add(linkItem);
                        }
                    }
                }
                foreach (var link in InternalLinks)
                {
                    if (link.FormListItem != null)
                    {
                        if (!link.FormListItem.IsActive)
                            removedLinks.Add(link);
                        else if (blocksWithPrototypeChanged.Contains(link.Source) || blocksWithPrototypeChanged.Contains(link.Target))
                        {
                            addedLinks.Add(link.FormListItem);
                            removedLinks.Add(link);
                        }
                    }
                    else if (!link.Source.FormListItem.IsActive || !link.Target.FormListItem.IsActive)
                        removedLinks.Add(link);
                }
                foreach (var link in removedLinks)
                {
                    RemoveLink(link);
                }

                removedBlocks.AddRange(InternalItems.Where(block => !block.FormListItem.IsActive));
                removedBlocks.AddRange(blocksWithPrototypeChanged);

                foreach (var block in removedBlocks)
                {
                    RemoveItem(block);
                }

                foreach (var block in modifiedBlocks)
                {
                    RestorePropertyLinks(block);   //restore actual AssetLinks on Diagram
                    block.UpdateConnectorFillColors();
                    block.UpdatePosition(transaction);
                }

                foreach (var linkItem in addedLinks)
                {
                    AddAnchorLinkToDiagram(linkItem);
                }

                if (addedBlocks.Count > 0)
                {
                    foreach (var block in InternalItems)
                        block.IsSelected = addedBlocks.Contains(block);
                }
            }
            finally
            {
                addedBlocks.Clear();
                modifiedBlocks.Clear();
                blocksWithPrototypeChanged.Clear();
                removedBlocks.Clear();
                addedLinks.Clear();
                removedLinks.Clear();
            }
        }

        /// <summary>
        /// Blocks and links adds to diagram using data from Form
        /// </summary>
        private void LoadContent()
        {
            foreach (var element in blockLists.SelectMany(element => element.Children.Cast<BlockListItem>()))
            {
                AddBlockToDiagram(element);
            }

            if (linkList != null)
            {
                foreach (var element in linkList.Children.Cast<LinkListItem>())
                {
                    AddAnchorLinkToDiagram(element);
                }
            }

            //set links of asset
            foreach (var source in InternalItems)
                RestorePropertyLinks(source);
        }

        #region Block API

        /// <summary>
        /// Blocks ViewModel made from forms element and add to Diagram
        /// </summary>
        private BlockBase AddBlockToDiagram(BlockListItem element)
        {
            currentSelected = BlockBase.Factory(element.Prototype, element);
            AddNode(currentSelected);
            return currentSelected;
        }

        private static Point AlignmentByConnector(Archetype archetype, IEnumerable<SchemaConnector> connectors, Point cursorPosition, ConnectorKind sourceConnectorKind)
        {
            var positionX = cursorPosition.X;
            var positionY = cursorPosition.Y;
            if (archetype != null)
            {
                var connector = sourceConnectorKind switch
                {
                    ConnectorKind.Out => connectors.First(x => x.Kind == ConnectorKind.In),
                    ConnectorKind.Property => connectors.First(x => x.Kind == ConnectorKind.Asset),
                    _ => throw new InvalidOperationException()
                };

                var connectorRelativePosition = connector.Position;
                positionX -= connectorRelativePosition.X * archetype.BlockSize.Width;
                positionY -= connectorRelativePosition.Y * archetype.BlockSize.Height;
            }

            return new Point(positionX, positionY);
        }

        /// <summary>
        /// JsonValue made from blocks ViewModel and add to From as element
        /// </summary>
        /// <returns>GUID of new block</returns>
        private string AddElementToForm(SchemaBlock prototype, Point position, ITransaction transaction)
        {
            var json = new JsonObject();
            var guid = Guid.NewGuid().ToString();
            json["ref"] = guid;
            json["layout"] = new JsonObject { { "x", position.X }, { "y", position.Y } };

            var listElement = GetListOfBlockByPrototype(prototype);

            if (listElement.ListType.ItemType is VariantSchemaType variantType)
            {
                var variantChild = variantType.Variant.Children.First(child => child.Block == prototype);
                json[variantType.Variant.Tag] = variantChild.TagValue!;
            }

            transaction.RequestFullInvalidation(); // TODO: why?
            listElement.PasteElement(json, transaction);
            return guid;
        }

        public void CreateBlock(SchemaBlock prototype, BaseConnector? sourceConnector, Point position)
        {
            if (sourceConnector != null)
            {
                if (sourceConnector.Kind != ConnectorKind.Out && sourceConnector.Kind != ConnectorKind.Property)
                    throw new ArgumentException("Unsupported source connector", nameof(sourceConnector));
                position = AlignmentByConnector(prototype.Archetype, prototype.Connectors, position, sourceConnector.Kind);
            }

            form.Run(transaction =>
            {
                var targetBlockRef = AddElementToForm(prototype, position, transaction);

                if (sourceConnector != null)
                {
                    if (sourceConnector.Kind == ConnectorKind.Out)
                    {
                        var targetConnector = prototype.Connectors.First(x => x.Kind == ConnectorKind.In);
                        AddAnchorLinkToForm(sourceConnector.BlockViewModel.GetRefValue(), sourceConnector.Name, targetBlockRef, targetConnector.Name, transaction);
                    }
                    else if (sourceConnector.Kind == ConnectorKind.Property)
                    {
                        ((PropertyConnector)sourceConnector).SetTargetId(targetBlockRef, transaction);
                    }
                }
            });
        }

        /// <summary>
        /// Get from Form list containing all blocks of diagram
        /// </summary>
        /// <param name="prototype"></param>
        /// <returns></returns>
        private ListElement GetListOfBlockByPrototype(SchemaBlock prototype)
        {
            var field = schemaDiagram.GetFieldByBlock(prototype);
            return (ListElement)form.Root.Record.Children.First(f => f.Name == field.Name).DeepElement;
        }

        #endregion Block API

        #region Link API

        public bool TryCreateLink(BaseConnector sourceConnector, BaseConnector targetConnector)
        {
            if (!string.IsNullOrEmpty(targetConnector.Category)
                && !string.IsNullOrEmpty(sourceConnector.Category)
                && targetConnector.Category != sourceConnector.Category)
                return false;

            if (sourceConnector.Kind == ConnectorKind.Out && targetConnector.Kind == ConnectorKind.In)
            {
                //check for duplicate any connection
                foreach (var link in InternalLinks)
                {
                    if (link.Source == sourceConnector.BlockViewModel && link.Target == targetConnector.BlockViewModel &&
                        link.SourceSlot == sourceConnector.Name && link.TargetSlot == targetConnector.Name)
                        return false;
                }

                form.Run(transaction =>
                {
                    AddAnchorLinkToForm(sourceConnector.BlockViewModel.GetRefValue(), sourceConnector.Name, targetConnector.BlockViewModel.GetRefValue(), targetConnector.Name, transaction);
                });
                return true;
            }
            else if (sourceConnector is PropertyConnector propertyConnector && targetConnector.Kind == ConnectorKind.Asset)
            {
                form.Run(transaction =>
                {
                    propertyConnector.SetTargetId(targetConnector.BlockViewModel.GetRefValue(), transaction);
                });
                return true;
            }
            else
                return false;
        }

        private void AddAnchorLinkToDiagram(LinkListItem element)
        {
            if (!element.IsValid)
                return;

            var first = InternalItems.SingleOrDefault(x => x.FormListItem.RefElement == element.FromRefElement.ValueElement);
            if (first == null)
                return;

            var second = InternalItems.SingleOrDefault(x => x.FormListItem.RefElement == element.ToRefElement.ValueElement);
            if (second == null)
                return;

            var link = new Link(first, second, element.FromSlotElement.Value, element.ToSlotElement.Value, element);
            AddLink(link);
        }

        private void RestorePropertyLinks(BlockBase source)
        {
            List<Link>? removeLinks = null;
            foreach (var link in InternalLinks)
            {
                if (link.Source == source && link.SourceConnector is PropertyConnector propertyConnector)
                {
                    if (!propertyConnector.ReadFromFormTargetIds().Contains(link.Target.GetRefValue())) // TODO: optimize
                    {
                        removeLinks ??= new List<Link>();
                        removeLinks.Add(link);
                    }
                }
            }

            if (removeLinks != null)
            {
                foreach (var link in removeLinks)
                {
                    RemoveLink(link);
                }
            }

            foreach (var propertyConnector in source.Connectors.OfType<PropertyConnector>())
            {
                foreach (var target in InternalItems.Where(x => propertyConnector.ReadFromFormTargetIds().Contains(x.FormListItem.RefElement.Value)).ToList())
                {
                    var targetAssetConnector = target.Connectors.FirstOrDefault(x => x is SimpleConnector { Kind: ConnectorKind.Asset } connector && (propertyConnector.Category == null || connector.Category == null || propertyConnector.Category == connector.Category));

                    if (targetAssetConnector == null)
                        continue;

                    if (InternalLinks.FirstOrDefault(link => link.Source == source && link.Target == target && link.SourceConnector == propertyConnector && link.TargetConnector == targetAssetConnector) != null)
                        continue;

                    var link = new Link(source, target, propertyConnector.Name, targetAssetConnector.Name);
                    AddLink(link);
                }
            }
        }

        private void AddAnchorLinkToForm(string sourceRef, string sourceConnectorName, string targetRef, string targetConnectorName, ITransaction transaction)
        {
            var linkJson = new JsonObject()
            {
                { "from", new JsonObject()
                    {
                        { "block", sourceRef },
                        { "slot", sourceConnectorName }
                    }
                },
                { "to", new JsonObject()
                    {
                        { "block", targetRef },
                        { "slot", targetConnectorName }
                    }
                }
            };

            linkList.PasteElement(linkJson, transaction);
        }

        #endregion Link API

        public ImmutableJsonObject CopySelection()
        {
            var result = new JsonBuilder(ImmutableJson.EmptyObject);
            foreach (var block in InternalItems)
            {
                if (block.IsSelected)
                {
                    var blockJson = block.FormListItem.Json;
                    var parentElement = block.FormListItem.GetParentByType<Field>()!;
                    var parentJsonArray = result.GetOrAdd(parentElement.Name).GetOrCreateArray();
                    parentJsonArray.Add(new JsonBuilder(blockJson));
                }
            }
            foreach (var link in InternalLinks)
            {
                if (link.IsSelected && link.FormListItem != null && link.Target.IsSelected && link.Source.IsSelected)
                {
                    var linkJsonArray = result.GetOrAdd("links").GetOrCreateArray();
                    linkJsonArray.Add(new JsonBuilder(link.FormListItem.Json));
                }
            }
            return result.ToImmutable().AsObject;
        }

        public void DeleteSelection()
        {
            var itemsToDelete = InternalItems.Where(block => block.IsSelected).Select(block => block.FormListItem).ToList();
            form.Run(transaction =>
            {
                foreach (var link in InternalLinks)
                {
                    var isDeletingSource = itemsToDelete.Contains(link.Source.FormListItem);
                    var isDeletingTarget = itemsToDelete.Contains(link.Target.FormListItem);
                    if (link.IsAssetLink && !isDeletingSource) // No special care is needed when properties are removed as part of the block
                    {
                        bool removeLink = isDeletingTarget || link.IsSelected;
                        if (removeLink && link.SourceConnector is PropertyConnector propertyConnector)
                        {
                            propertyConnector?.RemoveTargetId(link.Target.GetRefValue(), transaction);
                        }
                    }
                    if (!link.IsAssetLink && (link.IsSelected || isDeletingSource || isDeletingTarget))
                    {
                        linkList.Remove(link.FormListItem, transaction);
                    }
                }
                foreach (var item in itemsToDelete)
                {
                    item.List.Remove(item, transaction);
                }
            });
        }

        public void PasteSelection(ImmutableJson json)
        {
            if (!json.IsObject)
                return;
            var jsonObject = json.AsObject;
            var refMapping = GetNewRefMapping(jsonObject);
            form.Run(transaction =>
            {
                foreach (var pair in jsonObject)
                {
                    if (pair.Key != "links" && pair.Value.IsArray)
                    {
                        var blockList = blockLists.FirstOrDefault(b => b.GetParentByType<Field>().Name == pair.Key);
                        if (blockList != null)
                        {
                            foreach (var blockJson in pair.Value.AsArray)
                            {
                                if (blockJson.IsObject)
                                {
                                    var blockBuilder = new JsonBuilder(blockJson);
                                    var refBuilder = blockBuilder.Get("ref");
                                    if (refBuilder.Type == JsonType.String)
                                    {
                                        var blockRef = refBuilder.ToImmutable().AsString;
                                        if (refMapping.TryGetValue(blockRef, out var newRef))
                                            refBuilder.Set(newRef);
                                    }
                                    var layoutBuilder = blockBuilder.Get("layout");
                                    if (layoutBuilder.IsObject)
                                    {
                                        var layoutObject = layoutBuilder.AsObject;
                                        if (layoutObject.TryGetValue("x", out var xBuilder) && xBuilder.Type == JsonType.Number)
                                            xBuilder.Set(Math.Floor(xBuilder.ToImmutable().AsNumber + 30));
                                        if (layoutObject.TryGetValue("y", out var yBuilder) && yBuilder.Type == JsonType.Number)
                                            yBuilder.Set(Math.Floor(yBuilder.ToImmutable().AsNumber + 30));
                                    }

                                    SchemaRecord? record = blockList.ListType.ItemType switch
                                    {
                                        VariantSchemaType variantType =>
                                            blockJson.AsObject.TryGetValue(variantType.Variant.Tag, out var tagValue) && tagValue.IsString ?
                                                variantType.Variant.GetChild(tagValue.AsString) : null,
                                        RecordSchemaType recordType => recordType.Record,
                                        _ => null
                                    };

                                    if (record?.Block != null)
                                    {
                                        foreach (var connector in record.Block.Connectors)
                                        {
                                            if (connector.Kind == ConnectorKind.Property)
                                            {
                                                var fieldBuilder = blockBuilder.Get(connector.Field!);
                                                if (fieldBuilder.Type == JsonType.String)
                                                {
                                                    var fieldRef = fieldBuilder.ToImmutable().AsString;
                                                    if (refMapping.TryGetValue(fieldRef, out var newRef))
                                                        fieldBuilder.Set(newRef);
                                                }
                                                else if (fieldBuilder.IsArray)
                                                {
                                                    var fields = fieldBuilder.AsArray;
                                                    foreach (var field in fields)
                                                    {
                                                        if (field.Type == JsonType.String)
                                                        {
                                                            var fieldRef = field.ToImmutable().AsString;
                                                            if (refMapping.TryGetValue(fieldRef, out var newRef))
                                                                field.Set(newRef);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    blockList.PasteElement(blockBuilder.ToImmutable(), transaction);
                                }
                            }
                        }
                    }
                }

                if (linkList != null && jsonObject.TryGetValue("links", out var linksJson) && linksJson.IsArray)
                {
                    foreach (var linkJson in linksJson.AsArray)
                    {
                        var linkBuilder = new JsonBuilder(linkJson);
                        if (linkBuilder.TryFetch(new JsonPath("from", "block"), out var fromBlock) && fromBlock.Type == JsonType.String)
                        {
                            var fromRef = fromBlock.ToImmutable().AsString;
                            if (refMapping.TryGetValue(fromRef, out var newRef))
                                fromBlock.Set(newRef);
                        }
                        if (linkBuilder.TryFetch(new JsonPath("to", "block"), out var toBlock) && toBlock.Type == JsonType.String)
                        {
                            var toRef = toBlock.ToImmutable().AsString;
                            if (refMapping.TryGetValue(toRef, out var newRef))
                                toBlock.Set(newRef);
                        }
                        linkList.PasteElement(linkBuilder.ToImmutable(), transaction);
                    }
                }
            });
        }

        private IReadOnlyDictionary<string, string> GetNewRefMapping(ImmutableJsonObject json)
        {
            var result = new Dictionary<string, string>();
            foreach (var blockList in blockLists)
            {
                var listName = blockList.GetParentByType<Field>()!.Name;
                if (json.TryGetValue(listName, out var blocksJson) && blocksJson.IsArray)
                {
                    foreach (var block in blocksJson.AsArray)
                    {
                        if (block.IsObject && block.AsObject.TryGetValue("ref", out var refJson) && refJson.IsString)
                        {
                            var newRef = Guid.NewGuid().ToString();
                            result.Add(refJson.AsString, newRef);
                        }
                    }
                }
            }
            return result;
        }

        public void AutoLayout()
        {
            form.Run(transaction => new DiagramLayout().AutoLayout(this, transaction));
        }
    }
}