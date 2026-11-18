using Hercules.Shell;
using System;
using System.Windows.Input;

namespace Hercules.Replication
{
    public class SynchronizeDatabaseAdvancedFilterNotification : Notification
    {

        public string Filter
        {
            get;
            set => SetField(ref field, value);
        } = "";

        public bool IsRegex
        {
            get;
            set => SetField(ref field, value);
        } = false;

        private readonly Action<string, bool, bool, bool> Apply;

        public SynchronizeDatabaseAdvancedFilterNotification(Action<string, bool, bool, bool> apply) : base(null)
        {
            this.Apply = apply;
            CheckMatched = Commands.Execute(() => Apply(Filter, IsRegex, true, true)).If(() => !string.IsNullOrEmpty(Filter));
            UncheckMatched = Commands.Execute(() => Apply(Filter, IsRegex, true, false)).If(() => !string.IsNullOrEmpty(Filter));
            CheckUnmatched = Commands.Execute(() => Apply(Filter, IsRegex, false, true)).If(() => !string.IsNullOrEmpty(Filter));
            UncheckUnmatched = Commands.Execute(() => Apply(Filter, IsRegex, false, false)).If(() => !string.IsNullOrEmpty(Filter));
        }

        public ICommand CheckMatched { get; }
        public ICommand UncheckMatched { get; }
        public ICommand CheckUnmatched { get; }
        public ICommand UncheckUnmatched { get; }
    }
}
