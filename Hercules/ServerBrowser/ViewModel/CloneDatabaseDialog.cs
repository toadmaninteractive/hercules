using Hercules.Shell;
using System.Collections.Generic;
using System.Linq;

namespace Hercules.ServerBrowser
{
    public class CloneDatabaseDialog : ValidatedDialog
    {
        public IReadOnlyList<string> ExistingDatabases { get; }

        public CloneDatabaseDialog(IReadOnlyList<string> existingDatabases)
        {
            ExistingDatabases = existingDatabases;
        }

        public string Name
        {
            get;
            set => SetField(ref field, value);
        } = string.Empty;

        public string? NameError
        {
            get;
            set => SetField(ref field, value);
        }

        public string? Source
        {
            get;
            set => SetField(ref field, value);
        } = string.Empty;

        public bool CreateConnection
        {
            get;
            set => SetField(ref field, value);
        } = true;

        [PropertyValidator("Name")]
        public string? ValidateName()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                NameError = "Database Name is required";
                return NameError;
            }

            NameError = ExistingDatabases.Contains(Name) ? $"Database '{Name}' already exists" : null;
            return NameError;
        }

        [PropertyValidator("Source")]
        public string? ValidateSource()
        {
            if (string.IsNullOrWhiteSpace(Source))
                return null;
            else if (ExistingDatabases.Contains(Source))
                return null;
            else
                return $"Database '{Source}' does not exist";
        }
    }
}
