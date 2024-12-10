using Hercules.Forms.Elements;
using Hercules.Forms.Schema;
using System.Windows;

namespace Hercules.Diagrams
{
    public class BlockBehaviour : BlockBase
    {
        private Element? specialFieldText;

        public Element? SpecialFieldText
        {
            get => specialFieldText;
            set
            {
                if (specialFieldText != value)
                {
                    specialFieldText = value;
                    OnPropertyChanged(() => SpecialFieldText);
                }
            }
        }

        public BlockBehaviour(SchemaBlock prototype, BlockListItem element)
            : base(prototype, element)
        {
            this.VisibilityInOutConnectors = Visibility.Hidden;
        }

        public override void SetSpecialFields()
        {
            var path = this.Prototype.GetSpecialFieldPath("text");
            if (path != null)
                this.SpecialFieldText = this.FormListItem.GetByPath(path);
        }
    }
}