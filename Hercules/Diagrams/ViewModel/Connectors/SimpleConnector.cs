using Hercules.Forms.Schema;
using System.Windows;

namespace Hercules.Diagrams
{
    public class SimpleConnector : BaseConnector
    {
        public SimpleConnector(BlockBase blockViewModel, SchemaConnector schema, Visibility labelVisibility)
            : base(blockViewModel, schema, labelVisibility)
        {

        }
    }
}