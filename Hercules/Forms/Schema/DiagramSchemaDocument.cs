using Hercules.Diagrams;
using Igor.Schema;
using Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Hercules.Forms.Schema
{
    public sealed class DiagramSchemaDocument
    {
        private readonly IReadOnlyDictionary<string, SchemaDiagram> diagrams;

        public DiagramSchemaDocument(FormSchema formSchema, ImmutableJson json)
        {
            var diagramSchema = DiagramSchemaJsonSerializer.Instance.Deserialize(json);
            diagrams = InitDiagramSchema(formSchema, diagramSchema);
        }

        public bool IsDiagramEnabled(string? category)
        {
            return category != null && diagrams.ContainsKey(category);
        }

        public SchemaDiagram GetDiagram(string category)
        {
            return diagrams[category];
        }

        private static IReadOnlyDictionary<string, SchemaDiagram> InitDiagramSchema(FormSchema formSchema, DiagramSchema diagramSchema)
        {
            var result = new Dictionary<string, SchemaDiagram>();
            foreach (var prototype in diagramSchema.Prototypes)
            {
                var record = (SchemaRecord)formSchema.Structs[prototype.CustomType];
                if (record.Parent != null)
                    record.Parent.TagField.IsVisibleInPropertyEditor = false;
                var block = new SchemaBlock(
                    archetype: Archetypes.GetArchetype(prototype.Archetype),
                    name: prototype.Name,
                    caption: string.IsNullOrEmpty(prototype.Caption) ? prototype.Name : prototype.Caption,
                    iconName: prototype.Icon,
                    showIcon: prototype.ShowIcon ?? true,
                    color: prototype.Color == null ? (Color?)null : (Color)ColorConverter.ConvertFromString(prototype.Color),
                    specialFields: prototype.SpecialFields.ToDictionary(pair => pair.Value, pair => JsonPath.Parse(pair.Key)),
                    connectors: GetConnectors(record, prototype),
                    record: record);
                record.Block = block;
                block.RefField!.IsVisibleInPropertyEditor = false;
                ((StringSchemaType)block.RefField!.Type).ReferenceSourceId = "diagram_block_ref";
                ((StringSchemaType)block.RefField!.Type).UniqueReference = true;
                block.LayoutField!.IsVisibleInPropertyEditor = false;
            }

            foreach (var category in diagramSchema.DiagramTags)
            {
                var record = ((SchemaVariant)formSchema.Root).GetChild(category);
                var diagram = new SchemaDiagram(record);
                result.Add(category, diagram);

                foreach (var field in diagram.GetBlockFields())
                    field.IsVisibleInPropertyEditor = false;

                FillLinks(diagram);
            }

            return result;
        }

        private static List<SchemaConnector> GetConnectors(SchemaRecord record, Prototype prototype)
        {
            static Color? ColorFromString(string? colorName)
            {
                return string.IsNullOrEmpty(colorName) ? (Color?)null : (Color)ColorConverter.ConvertFromString(colorName);
            }

            List<SchemaConnector> connectors = prototype.Connectors.Select(c =>
                new SchemaConnector(c.Name, ConnectorTypeToKind(c.Type), Point.Parse(c.Position), c.Field, c.Caption, ColorFromString(c.Color), c.Category)).ToList();

            foreach (var connector in connectors)
                if (connector.Kind == ConnectorKind.Property)
                {
                    var recordField = record.AllFields.First(f => f.Name == connector.Field);
                    recordField.IsVisibleInPropertyEditor = false;
                    if (recordField.Type is ListSchemaType { ItemType: StringSchemaType stringItemSchemaType })
                    {
                        stringItemSchemaType.Species = StringSchemaType.StringSpecies.RefAsset;
                        stringItemSchemaType.ReferenceTargetId = "diagram_block_ref";
                        stringItemSchemaType.RequireValidReference = true;
                    }
                    else if (recordField.Type is StringSchemaType stringSchemaType)
                    {
                        stringSchemaType.Species = StringSchemaType.StringSpecies.RefAsset;
                        stringSchemaType.ReferenceTargetId = "diagram_block_ref";
                        stringSchemaType.RequireValidReference = true;
                    }
                    else
                        throw new ArgumentException("Unexpected connector property type");
                }

            return connectors;
        }

        private static ConnectorKind ConnectorTypeToKind(ConnectorType type)
        {
            return type switch
            {
                ConnectorType.Asset => ConnectorKind.Asset,
                ConnectorType.Property => ConnectorKind.Property,
                ConnectorType.In => ConnectorKind.In,
                ConnectorType.Out => ConnectorKind.Out,
                _ => ConnectorKind.Invalid
            };
        }

        private static void FillLinks(SchemaDiagram diagram)
        {
            if (diagram.LinksField != null)
            {
                diagram.LinksField.IsVisibleInPropertyEditor = false;

                var itemType = (RecordSchemaType)((ListSchemaType)diagram.LinksField.Type).ItemType;
                itemType.Record.IsLink = true;

                var fromField = itemType.AllFields.First(field => field.Name == "from");
                var toField = itemType.AllFields.First(field => field.Name == "to");

                var blockFromField = ((RecordSchemaType)fromField.Type).AllFields.First(field => field.Name == "block");
                var slotFromField = ((RecordSchemaType)fromField.Type).AllFields.First(field => field.Name == "slot");
                ((StringSchemaType)blockFromField.Type).Species = StringSchemaType.StringSpecies.RefLink;
                ((StringSchemaType)blockFromField.Type).ReferenceTargetId = "diagram_block_ref";
                ((StringSchemaType)blockFromField.Type).RequireValidReference = true;
                ((StringSchemaType)slotFromField.Type).Species = StringSchemaType.StringSpecies.Slot;
            }
        }
    }
}
