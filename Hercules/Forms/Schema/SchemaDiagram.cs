using Hercules.Diagrams;
using Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Hercules.Forms.Schema
{
    public enum ConnectorKind
    {
        In,
        Out,
        Property,
        Asset,
        Invalid
    }

    public class SchemaDiagram
    {
        public SchemaRecord Record { get; }

        public IReadOnlyList<SchemaBlock> Palette { get; }

        public SchemaField LinksField { get; }

        public SchemaField GetFieldByBlock(SchemaBlock block)
        {
            foreach (var field in Record.AllFields)
            {
                if (field.Type is ListSchemaType listType)
                {
                    var itemType = listType.ItemType;
                    if (itemType is VariantSchemaType variantType)
                    {
                        foreach (var record in variantType.Variant.Children)
                        {
                            if (record.Block == block)
                                return field;
                        }
                    }
                    else if (itemType is RecordSchemaType recordType)
                    {
                        if (recordType.Record.Block == block)
                            return field;
                    }
                }
            }
            throw new ArgumentException("Block not found: " + block.Name);
        }

        public IEnumerable<SchemaField> GetBlockFields()
        {
            foreach (var field in Record.AllFields)
            {
                if (field.Type is ListSchemaType listType)
                {
                    var itemType = listType.ItemType;
                    if (itemType is VariantSchemaType variantType)
                    {
                        foreach (var record in variantType.Variant.Children)
                        {
                            if (record.Block != null)
                            {
                                yield return field;
                                break;
                            }
                        }
                    }
                    else if (itemType is RecordSchemaType recordType)
                    {
                        if (recordType.Record.Block != null)
                            yield return field;
                    }
                }
            }
        }

        List<SchemaBlock> CreatePalette()
        {
            var palette = new List<SchemaBlock>();
            foreach (var listType in Record.AllFields.Select(field => field.Type).OfType<ListSchemaType>())
            {
                var itemType = listType.ItemType;
                if (itemType is VariantSchemaType variantType)
                {
                    foreach (var record in variantType.Variant.Children)
                    {
                        if (record.Block != null)
                            palette.Add(record.Block);
                    }
                }
                else if (itemType is RecordSchemaType recordType)
                {
                    if (recordType.Record.Block != null)
                        palette.Add(recordType.Record.Block);
                }
            }
            return palette;
        }

        public SchemaDiagram(SchemaRecord record)
        {
            Record = record;
            LinksField = record.AllFields.FirstOrDefault(field => field.Name == "links");
            Palette = CreatePalette();
        }
    }

    public class SchemaBlock
    {
        public Archetype Archetype { get; }
        public string Name { get; }
        public string Caption { get; }
        public bool ShowIcon { get; }
        public Color? Color { get; }
        public string? IconName { get; }
        public IReadOnlyList<SchemaConnector> Connectors { get; }

        /// <summary>
        /// Whether this block can be anchor link source or target
        /// </summary>
        public bool HasAnchorConnectors { get; }

        /// <summary>
        /// Whether this block can be asset link target
        /// </summary>
        public bool IsAsset { get; }

        IReadOnlyDictionary<string, JsonPath> SpecialFields { get; }
        public SchemaRecord? Record { get; }
        public SchemaField? LayoutField { get; }
        public SchemaField? RefField { get; }

        public static readonly SchemaBlock InvalidBlock = new SchemaBlock(
            archetype: Archetypes.Invalid,
            name: "Invalid",
            caption: "Invalid",
            iconName: null,
            showIcon: true,
            color: null,
            specialFields: new Dictionary<string, JsonPath>(),
            connectors: new List<SchemaConnector> { SchemaConnector.InvalidConnector });

        public SchemaBlock(Archetype archetype, string name, string caption, string? iconName, bool showIcon, Color? color, IReadOnlyDictionary<string, JsonPath> specialFields, IReadOnlyList<SchemaConnector> connectors, SchemaRecord? record = null)
        {
            Archetype = archetype;
            Name = name;
            Caption = caption;
            IconName = iconName ?? archetype.DefaultIconName;
            ShowIcon = showIcon;
            Color = color;
            SpecialFields = specialFields;
            Connectors = connectors;
            Record = record;
            IsAsset = connectors.Any(c => c.Kind == ConnectorKind.Asset);
            HasAnchorConnectors = connectors.Any(c => c.Kind == ConnectorKind.Out || c.Kind == ConnectorKind.In);
            if (record != null)
            {
                LayoutField = record.AllFields.FirstOrDefault(f => f.Name == "layout");
                RefField = record.AllFields.FirstOrDefault(f => f.Name == "ref");
            }
        }

        public JsonPath? GetSpecialFieldPath(string specialField)
        {
            SpecialFields.TryGetValue(specialField, out var result);
            return result;
        }
    }

    public class SchemaConnector
    {
        public static readonly Color DefaultColor = (Color)ColorConverter.ConvertFromString("#FF5B7199");

        public string Name { get; }
        public ConnectorKind Kind { get; }
        public Point Position { get; }
        public string? Field { get; }
        public string Caption { get; }
        public Color Color { get; }
        public string? Category { get; }

        public SchemaConnector(string name, ConnectorKind kind, Point position, string? field, string? caption, Color? color, string? category)
        {
            Name = name;
            Kind = kind;
            Position = position;
            Field = field;
            Caption = caption ?? name;
            Color = color ?? DefaultColor;
            Category = category;
        }

        public static readonly SchemaConnector InvalidConnector = new SchemaConnector("INVALID", ConnectorKind.Invalid, new Point(0.5, 0.5), null, null, Colors.Red, null);
    }
}
