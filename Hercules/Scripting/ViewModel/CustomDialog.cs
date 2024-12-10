using Hercules.Forms;
using Hercules.Shell;
using System.Collections.Generic;
using System.Linq;

namespace Hercules.Scripting
{
    public class CustomDialog : Dialog
    {
        public IReadOnlyList<CustomDialogField> Fields { get; }
        public double FieldWidth { get; }

        public CustomDialog(string title, IReadOnlyList<CustomDialogField> fields, TextSizeService textSizeService)
        {
            Title = title;
            Fields = fields;
            FieldWidth = Fields.Max(f => textSizeService.GetWidth(f.Caption));
        }
    }
}
