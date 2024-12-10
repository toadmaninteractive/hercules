using Hercules.Documents;
using Hercules.Shell;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Hercules.Search
{
    public class SearchTool : Tool
    {
        public ICommand ClearCategoryFilterCommand { get; }
        public ICommand ClearSearchTextCommand { get; }
        public IReadOnlyObservableValue<Project?> ProjectObservable { get; }
        public ICommand ExecuteSearchCommand { get; }
        public bool MatchCase
        {
            get => matchCase;
            set => SetField(ref matchCase, value);
        }
        public bool OpenedDocuments
        {
            get => openedDocuments;
            set => SetField(ref openedDocuments, value);
        }
        public ObservableCollection<Category> SearchCategories { get; }
        public bool SearchEnums
        {
            get => searchEnums;
            set => SetField(ref searchEnums, value);
        }
        public bool SearchFields
        {
            get => searchFields;
            set => SetField(ref searchFields, value);
        }
        public bool SearchKeys
        {
            get => searchKeys;
            set => SetField(ref searchKeys, value);
        }
        public SearchModule SearchModule { get; }
        public bool SearchNumbers
        {
            get => searchNumbers;
            set => SetField(ref searchNumbers, value);
        }
        public bool SearchText
        {
            get => searchText;
            set => SetField(ref searchText, value);
        }
        public string Text
        {
            get => text;
            set => SetField(ref text, value);
        }
        public bool WholeWord
        {
            get => wholeWord;
            set => SetField(ref wholeWord, value);
        }

        bool matchCase;
        bool openedDocuments = false;
        bool searchEnums = true;
        bool searchFields = false;
        bool searchKeys = true;
        bool searchNumbers = true;
        bool searchText = true;
        string text = string.Empty;
        bool wholeWord;

        public SearchTool(SearchModule searchModule, IReadOnlyObservableValue<Project?> projectObservable)
        {
            this.ProjectObservable = projectObservable;
            this.SearchModule = searchModule;
            this.Title = "Search";
            this.ContentId = "{Search}";
            this.Pane = "RightToolsPane";
            this.IsVisible = false;
            this.ClearSearchTextCommand = Commands.Execute(() => Text = string.Empty);
            this.ExecuteSearchCommand = Commands.Execute(ExecuteSearch).If(() => projectObservable.Value?.SchemafulDatabase.Schema != null);
            this.SearchCategories = new ObservableCollection<Category>();

            this.ClearCategoryFilterCommand = Commands.Execute(() => SearchCategories.Clear());
        }

        private void ExecuteSearch()
        {
            var project = ProjectObservable.Value!;
            var docs = SearchCategories.Any() ? SearchCategories.SelectMany(c => c.Documents) : project.SchemafulDatabase.SchemafulDocuments;
            if (OpenedDocuments)
                docs = docs.Where(d => d.Editor != null);
            var search = new CustomSearchVisitor
            {
                Text = this.Text,
                SearchText = this.SearchText,
                SearchEnums = this.SearchEnums,
                SearchKeys = this.SearchKeys,
                SearchNumbers = this.SearchNumbers,
                SearchFields = this.SearchFields,
                MatchCase = this.MatchCase,
                WholeWord = this.WholeWord,
            };
            search.Search(project.SchemafulDatabase.Schema!, docs);
            SearchModule.ShowSearchResults(search.Results);
        }
    }
}
