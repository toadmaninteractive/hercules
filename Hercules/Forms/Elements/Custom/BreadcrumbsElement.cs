using Hercules.Forms.Presentation;
using Hercules.Forms.Schema;
using Hercules.Forms.Schema.Custom;
using Json;
using System.Collections.Generic;

namespace Hercules.Forms.Elements
{
    public class BreadcrumbsElement : CustomProxy<BreadcrumbsSchemaType>
    {
        public BreadcrumbsElement(IContainer parent, BreadcrumbsSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, type, json, originalJson, transaction)
        {
        }

        public IReadOnlyList<BreadcrumbsData> Items => CustomType.Editor.Items;

        public override void Present(PresentationContext context)
        {
            var proxy = context.GetProxy(this);
            context.AddItem(proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool(GetType())), height: 25);
        }
    }
}