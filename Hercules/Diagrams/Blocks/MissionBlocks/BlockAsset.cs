using Hercules.Forms.Elements;
using Hercules.Forms.Schema;

namespace Hercules.Diagrams
{
    public class BlockAsset : BlockBase
    {
        private Element? specialFieldName;

        public Element? SpecialFieldName
        {
            get => specialFieldName;
            set
            {
                if (specialFieldName != value)
                {
                    specialFieldName = value;
                    OnPropertyChanged(() => SpecialFieldName);
                }
            }
        }

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

        public BlockAsset(SchemaBlock prototype, BlockListItem element)
            : base(prototype, element)
        {
        }

        public override void SetSpecialFields()
        {
            var pathName = this.Prototype.GetSpecialFieldPath("name");
            if (pathName != null)
                this.SpecialFieldName = this.FormListItem.GetByPath(pathName);

            var pathText = this.Prototype.GetSpecialFieldPath("text");
            if (pathText != null)
                this.SpecialFieldText = this.FormListItem.GetByPath(pathText);
        }
    }
}