using System.Windows.Input;

namespace Hercules
{
    public static class RoutedCommands
    {
        public static readonly RoutedUICommand Find =
            new("Find", "Find", typeof(RoutedCommands), new InputGestureCollection { new KeyGesture(Key.F, ModifierKeys.Control) });

        public static readonly RoutedUICommand FindNext =
            new("Find Next", "FindNext", typeof(RoutedCommands), new InputGestureCollection { new KeyGesture(Key.F3) });

        public static readonly RoutedUICommand FindPrevious =
            new("Find Previous", "FindPrevious", typeof(RoutedCommands), new InputGestureCollection { new KeyGesture(Key.F3, ModifierKeys.Shift) });

        public static readonly RoutedUICommand ExpandSelection =
            new("Expand Selection", "ExpandSelection", typeof(RoutedCommands), new InputGestureCollection { new KeyGesture(Key.Add) });

        public static readonly RoutedUICommand CollapseSelection =
            new("Collapse Selection", "CollapseSelection", typeof(RoutedCommands), new InputGestureCollection { new KeyGesture(Key.Subtract) });

        public static readonly RoutedUICommand ExpandAll =
            new("Expand All", "ExpandAll", typeof(RoutedCommands), new InputGestureCollection { new KeyGesture(Key.Add, ModifierKeys.Control) });

        public static readonly RoutedUICommand CollapseAll =
            new("Collapse All", "CollapseAll", typeof(RoutedCommands), new InputGestureCollection { new KeyGesture(Key.Subtract, ModifierKeys.Control) });

        public static readonly RoutedUICommand CopyPath =
            new("Copy Path", "CopyPath", typeof(RoutedCommands), new InputGestureCollection { new KeyGesture(Key.P, ModifierKeys.Control) });

        public static readonly RoutedUICommand CopyFullPath =
            new("Copy Full Path", "CopyFullPath", typeof(RoutedCommands), new InputGestureCollection { new KeyGesture(Key.P, ModifierKeys.Shift | ModifierKeys.Control) });

        public static readonly RoutedUICommand DuplicateItem =
            new("Duplicate", "DuplicateItem", typeof(RoutedCommands), new InputGestureCollection { new KeyGesture(Key.D, ModifierKeys.Control) });

        public static readonly RoutedUICommand Clear =
            new("Clear", "Clear", typeof(RoutedCommands));

        public static readonly RoutedUICommand PasteChild =
            new("Paste Child", "PasteChild", typeof(RoutedCommands));

        public static readonly RoutedUICommand GoToJson =
            new("Go To Json", "GoToJson", typeof(RoutedCommands), new InputGestureCollection { new KeyGesture(Key.F7) });

        public static readonly RoutedUICommand GoToForm =
            new("Go To Form", "GoToForm", typeof(RoutedCommands));

        public static readonly RoutedUICommand OpenShortcut =
            new("Open Shortcut", "OpenShortcut", typeof(RoutedCommands));

        public static readonly RoutedUICommand ViewRevision =
            new("View Revision", "ViewRevision", typeof(RoutedCommands));

        public static readonly RoutedUICommand LoadRevision =
            new("Load Revision", "LoadRevision", typeof(RoutedCommands));

        public static readonly RoutedUICommand OpenFile =
            new("Open", "OpenFile", typeof(RoutedCommands));
    }
}
