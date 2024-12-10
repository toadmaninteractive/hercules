using Hercules.Shell;
using Json;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Hercules.Documents.Editor
{
    public class AddCustomFieldNotification : ValidatedNotification
    {
        string? name;

        [Required]
        [RegularExpression(@"^[a-z]{1}[a-z0-9_]*$", ErrorMessage = "Invalid field name")]
        public string? Name
        {
            get => name;
            set => SetField(ref name, value);
        }

        [PropertyValidator(nameof(Name))]
        public string? ValidateName()
        {
            if (Tab.Form.Root.Record.Children.Any(c => c.Name == Name))
                return "Field already exists";
            if (Tab.Form.Root.SchemalessFields.Any(c => c.Name == Name))
                return "Field already exists";
            return null;
        }

        private async Task AddCustomField()
        {
            Close();
            Tab.Form.Run(transaction => Tab.Form.Root.AddSchemalessField(name!, string.Empty, transaction));
            await Task.Delay(10).ConfigureAwait(true);
            Tab.GoToPath(new JsonPath(name!));
        }

        public ICommand ApplyCommand { get; }

        public DocumentFormTab Tab { get; }

        public AddCustomFieldNotification(DocumentFormTab tab)
            : base(null)
        {
            Tab = tab;
            ApplyCommand = Commands.ExecuteAsync(AddCustomField).If(() => string.IsNullOrEmpty(Error));
        }
    }
}
