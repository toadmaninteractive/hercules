using Hercules.Shell;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace Hercules.Documents.Editor
{
    public class ConvertCategoryNotification : Notification
    {
        public IReadOnlyList<Category> Categories { get; }

        Category? category;

        public Category? Category
        {
            get => category;
            set => SetField(ref category, value);
        }

        public ICommand ApplyCategoryCommand { get; private set; }

        public ConvertCategoryNotification(IReadOnlyList<Category> categories, Action<Category> applyCategory)
            : base(null)
        {
            this.Categories = categories;
            ApplyCategoryCommand = Commands.Execute(() => { Close(); applyCategory(Category!); }).If(() => Category != null);
        }
    }
}
