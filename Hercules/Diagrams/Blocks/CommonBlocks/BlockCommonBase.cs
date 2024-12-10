using Hercules.Forms.Elements;
using Hercules.Forms.Schema;

namespace Hercules.Diagrams
{
    public class BlockCommonBase : BlockBase
    {
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

        public Element? SpecialFieldTime
        {
            get => specialFieldTime;
            set
            {
                if (specialFieldTime != value)
                {
                    specialFieldTime = value;
                    OnPropertyChanged(() => SpecialFieldTime);
                }
            }
        }

        private Element? specialFieldText;
        private Element? specialFieldTime;

        public BlockCommonBase(SchemaBlock prototype, BlockListItem element) : base(prototype, element)
        {
        }

        public override void SetSpecialFields()
        {
            var pathTime = this.Prototype.GetSpecialFieldPath("time");
            if (pathTime != null)
                this.SpecialFieldTime = this.FormListItem.GetByPath(pathTime);

            var pathText = this.Prototype.GetSpecialFieldPath("text");
            if (pathText != null)
                this.SpecialFieldText = this.FormListItem.GetByPath(pathText);
        }
    }
}