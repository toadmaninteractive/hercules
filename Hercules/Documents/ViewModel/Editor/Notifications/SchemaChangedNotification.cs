using Hercules.Shell;
using System;
using System.Windows.Input;

namespace Hercules.Documents.Editor
{
    public class SchemaChangedNotification : Notification
    {
        public ICommand ApplySchemaCommand { get; private set; }

        public SchemaChangedNotification(Action applySchema)
            : base(null)
        {
            ApplySchemaCommand = Commands.Execute(DoAndClose(applySchema));
        }
    }
}
