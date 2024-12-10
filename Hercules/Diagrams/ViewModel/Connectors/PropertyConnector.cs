using Hercules.Forms.Elements;
using Hercules.Forms.Schema;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Hercules.Diagrams
{
    public abstract class PropertyConnector : BaseConnector
    {
        protected PropertyConnector(BlockBase block, SchemaConnector schema, Visibility labelVisibility)
            : base(block, schema, labelVisibility)
        {
            AssetFieldPath = schema.Field!;
        }

        public string AssetFieldPath { get; }

        public abstract void SetTargetId(string targetId, ITransaction transaction);

        public abstract IEnumerable<string> ReadFromFormTargetIds();

        public abstract void RemoveTargetId(string targetId, ITransaction transaction);

        public abstract bool IsValid();

        public void UpdateFillColor()
        {
            FillColor = IsValid() ? new SolidColorBrush(BaseFillColor) : new SolidColorBrush(Colors.Red);
        }
    }
}