using Hercules.Shell;
using System;
using System.Windows.Input;

namespace Hercules.Replication
{
    public class SynchronizeDatabaseAdvancedFilterNotification : Notification
    {
        private string filter = "";

        public string Filter
        {
            get => filter;
            set => SetField(ref filter, value);
        }

        public bool IsRegex
        {
            get;
            set => SetField(ref field, value);
        } = false;

        private readonly Action<string, bool, bool, bool> Apply;

        public SynchronizeDatabaseAdvancedFilterNotification(Action<string, bool, bool, bool> apply) : base(null)
        {
            this.Apply = apply;
            CheckMatched = Commands.Execute(() => Apply(Filter, IsRegex, true, true)).If(() => !string.IsNullOrEmpty(filter));
            UncheckMatched = Commands.Execute(() => Apply(Filter, IsRegex, true, false)).If(() => !string.IsNullOrEmpty(filter));
            CheckUnmatched = Commands.Execute(() => Apply(Filter, IsRegex, false, true)).If(() => !string.IsNullOrEmpty(filter));
            UncheckUnmatched = Commands.Execute(() => Apply(Filter, IsRegex, false, false)).If(() => !string.IsNullOrEmpty(filter));
        }

        public ICommand CheckMatched { get; }
        public ICommand UncheckMatched { get; }
        public ICommand CheckUnmatched { get; }
        public ICommand UncheckUnmatched { get; }
    }
}
