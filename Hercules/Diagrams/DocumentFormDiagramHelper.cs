using Hercules.Forms.Elements;
using Hercules.Forms.Schema;
using Json;
using System.Collections.Generic;
using System.Linq;

namespace Hercules.Diagrams
{
    public static class DocumentFormDiagramHelper
    {
        public static IReadOnlyList<ListElement> GetBlockLists(this DocumentForm form)
        {
            return form.Root.Record.Children.Select(c => c.DeepElement).OfType<ListElement>().
                Where(element => IsBlockType(element.ListType.ItemType)).ToList();
        }

        public static bool IsBlockType(this SchemaType schemaType)
        {
            return schemaType switch
            {
                VariantSchemaType variantType => variantType.Variant.IsBlock,
                RecordSchemaType recordType => (recordType.Record.Block != null),
                _ => false
            };
        }

        public static IEnumerable<BlockListItem> GetAllBlockElements(this DocumentForm form)
        {
            return form.GetBlockLists().SelectMany(element => element.Children.Cast<BlockListItem>());
        }

        public static ListElement? GetLinkList(this DocumentForm form)
        {
            return form.Root.GetByPath(new JsonPath("links")) as ListElement;
        }
    }
}
