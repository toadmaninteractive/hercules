using Hercules.Forms.Elements;
using Hercules.Forms.Schema;
using Json;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Hercules.Diagrams
{
    public class PropertySingleConnector : PropertyConnector
    {
        public PropertySingleConnector(BlockBase block, SchemaConnector schema, Visibility labelVisibility)
            : base(block, schema, labelVisibility)
        {
        }

        public override void SetTargetId(string targetId, ITransaction transaction)
        {
            RefElement refElement = GetAssetField();
            if (refElement.Parent is OptionalElement optional)
                optional.SetIsSet(true, transaction);
            refElement.SetValue(targetId, transaction);
        }

        public override void RemoveTargetId(string targetId, ITransaction transaction)
        {
            RefElement refElement = GetAssetField();
            refElement.SetValue(string.Empty, transaction);
            if (refElement.Parent is OptionalElement { IsSet: true } optionalElement)
            {
                optionalElement.SetIsSet(false, transaction);
            }
        }

        public override IEnumerable<string> ReadFromFormTargetIds()
        {
            var field = BlockViewModel.FormListItem.GetByPath(new JsonPath(AssetFieldPath));

            if (field is RefElement refElement)
            {
                if (refElement.Value != null)
                    yield return refElement.Value;
            }
            else if (field is OptionalElement { IsSet: true } optionalElement)
            {
                var element = optionalElement.Element as RefElement;
                var value = element?.Value;
                if (value != null)
                    yield return value;
            }
        }

        public override bool IsValid()
        {
            var s = BlockViewModel.FormListItem.GetByPath(new JsonPath(AssetFieldPath));
            return s.IsValid;
        }

        private RefElement GetAssetField()
        {
            var field = BlockViewModel.FormListItem.GetByPath(new JsonPath(AssetFieldPath));

            if (field is RefElement refElement)
                return refElement;
            else if (field is OptionalElement optionalElement)
                return optionalElement.Element as RefElement;

            throw new InvalidOperationException("Invalid type of asset field");
        }
    }
}