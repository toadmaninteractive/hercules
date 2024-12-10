using Hercules.Forms.Elements;
using Hercules.Forms.Schema;
using Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Hercules.Diagrams
{
    public class PropertyListConnector : PropertyConnector
    {
        public PropertyListConnector(BlockBase block, SchemaConnector schema, Visibility labelVisibility)
            : base(block, schema, labelVisibility)
        {
        }

        public override void SetTargetId(string targetId, ITransaction transaction)
        {
            if (BlockViewModel.FormListItem.GetByPath(new JsonPath(AssetFieldPath)) is ListElement field)
                field.PasteElement(targetId, transaction);
        }

        public override void RemoveTargetId(string targetId, ITransaction transaction)
        {
            var field = BlockViewModel.FormListItem.GetByPath(new JsonPath(AssetFieldPath)) as ListElement;
            if (field == null)
                return;

            var removeItem = field.Children.FirstOrDefault(x => (x.DeepElement as RefElement).Value == targetId);
            if (removeItem == null)
                return;

            field.Remove(removeItem, transaction);
        }

        public override bool IsValid()
        {
            var field = (ListElement)BlockViewModel.FormListItem.GetByPath(new JsonPath(AssetFieldPath));
            return field.IsValid;
        }

        public override IEnumerable<string> ReadFromFormTargetIds()
        {
            if (BlockViewModel.FormListItem.GetByPath(new JsonPath(AssetFieldPath)) is ListElement field)
                return field.Children.OfType<ListItem>().Select(x => (x.DeepElement as RefElement).Value).ToList();
            throw new NotImplementedException();
        }
    }
}