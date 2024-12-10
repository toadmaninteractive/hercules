using Hercules.Shell;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace Hercules.Summary
{
    public class ImportColumn : NotifyPropertyChanged
    {
        public string Name { get; }

        bool isChecked;

        public bool IsChecked
        {
            get => isChecked;
            set => SetField(ref isChecked, value);
        }

        public TableColumn? TableColumn { get; }
        public int Index { get; }

        public ImportColumn(string name, int index, bool isChecked, TableColumn? tableColumn)
        {
            Name = name;
            Index = index;
            IsChecked = isChecked;
            TableColumn = tableColumn;
        }
    }

    public class ImportColumnsDialog : Dialog
    {
        public ImportColumnsDialog(IReadOnlyList<ImportColumn> columns, TimeZoneInfo defaultTimeZone)
        {
            this.Columns = columns;
            this.CheckAllCommand = Commands.Execute(CheckAll);
            this.UncheckAllCommand = Commands.Execute(UncheckAll);
            SetLocalTimeZoneCommand = Commands.Execute(() => TimeZone = TimeZoneInfo.Local);
            SetUtcTimeZoneCommand = Commands.Execute(() => TimeZone = TimeZoneInfo.Utc);
            timeZone = defaultTimeZone;
        }

        public IReadOnlyList<ImportColumn> Columns { get; }

        public ICommand CheckAllCommand { get; }
        public ICommand UncheckAllCommand { get; }

        bool allowNewDocuments;

        public bool AllowNewDocuments
        {
            get => allowNewDocuments;
            set => SetField(ref allowNewDocuments, value);
        }

        TimeZoneInfo timeZone;
        public TimeZoneInfo TimeZone
        {
            get => timeZone;
            set => SetField(ref timeZone, value);
        }

        public ICommand SetLocalTimeZoneCommand { get; }
        public ICommand SetUtcTimeZoneCommand { get; }

        void CheckAll()
        {
            foreach (var col in Columns)
                col.IsChecked = true;
        }

        void UncheckAll()
        {
            foreach (var col in Columns)
                col.IsChecked = false;
        }
    }
}
