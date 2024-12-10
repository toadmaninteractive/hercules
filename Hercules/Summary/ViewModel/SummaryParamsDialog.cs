using Hercules.Documents;
using Hercules.Shell;
using System.Windows.Input;

namespace Hercules.Summary
{
    public class SummaryParamsDialog : ValidatedDialog
    {
        public SchemafulDatabase SchemafulDatabase { get; }

        public SummaryParamsDialog(SchemafulDatabase schemafulDatabase, Category? category, Structure? structure)
        {
            this.SchemafulDatabase = schemafulDatabase;
            this.category = category;
            if (structure == null)
                UpdateFields();
            else
                this.Structure = structure;
            this.Title = "Summary Table Columns";
            this.CheckAllCommand = Commands.Execute(() => CheckAll(true)).If(() => Structure != null);
            this.UncheckAllCommand = Commands.Execute(() => CheckAll(false)).If(() => Structure != null);
        }

        public ICommand CheckAllCommand { get; }
        public ICommand UncheckAllCommand { get; }

        Category? category;

        public Category? Category
        {
            get => category;
            set
            {
                if (category != value)
                {
                    category = value;
                    RaisePropertyChanged();
                    UpdateFields();
                }
            }
        }

        private string? сategoryError;

        public string? CategoryError
        {
            get => сategoryError;
            set => SetField(ref сategoryError, value);
        }

        [PropertyValidator(nameof(Category))]
        public string? ValidateCategory()
        {
            CategoryError = Category == null ? "Category is required" : null;
            return CategoryError;
        }

        Structure? structure;

        public Structure? Structure
        {
            get => structure;
            set => SetField(ref structure, value);
        }

        void UpdateFields()
        {
            if (category == null)
                Structure = null;
            else
                Structure = new Structure(SchemafulDatabase.Schema, category.Name);
        }

        void CheckAll(bool check)
        {
            Structure!.Visit(item =>
                {
                    if (item is StructureValue value)
                        value.IsChecked = check;
                    return item is StructureCategory { IsExpanded: true };
                });
        }
    }
}
