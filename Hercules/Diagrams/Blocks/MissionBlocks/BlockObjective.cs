using Hercules.Forms.Elements;
using Hercules.Forms.Schema;

namespace Hercules.Diagrams
{
    public class BlockObjective : BlockBase
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

        public BlockObjective(SchemaBlock prototype, BlockListItem element)
            : base(prototype, element)
        {
        }

        public override void SetSpecialFields()
        {
            var path = this.Prototype.GetSpecialFieldPath("text");
            if (path != null)
                this.SpecialFieldText = this.FormListItem.GetByPath(path);
        }
    }
}