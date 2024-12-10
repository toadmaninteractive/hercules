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

        string name = string.Empty;

        public string Name
        {
            get => name;
            set => SetField(ref name, value);
        }

        private string? nameError;

        public string? NameError
        {
            get => nameError;
            set => SetField(ref nameError, value);
        }

        string? source = string.Empty;

        public string? Source
        {
            get => source;
            set => SetField(ref source, value);
        }

        bool createConnection = true;

        public bool CreateConnection
        {
            get => createConnection;
            set => SetField(ref createConnection, value);
        }

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
