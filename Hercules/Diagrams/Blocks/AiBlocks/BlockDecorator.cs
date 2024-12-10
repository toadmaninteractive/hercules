using Hercules.Forms.Elements;
using Hercules.Forms.Schema;

namespace Hercules.Diagrams
{
    public class BlockDecorator : BlockBase
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

        public BlockDecorator(SchemaBlock prototype, BlockListItem element)
            : base(prototype, element)
        {
            this.VisibilityInOutConnectors = System.Windows.Visibility.Hidden;
            this.VisibilityPropertyConnectors = System.Windows.Visibility.Hidden;
        }

        public override void SetSpecialFields()
        {
            var path = this.Prototype.GetSpecialFieldPath("text");
            if (path != null)
                this.SpecialFieldText = this.FormListItem.GetByPath(path);
        }
    }
}
