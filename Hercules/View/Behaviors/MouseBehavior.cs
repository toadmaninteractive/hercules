using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Hercules.Controls
{
    public class MouseBehavior
    {
        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.RegisterAttached("CommandParameter", typeof(object), typeof(MouseBehavior), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty DoubleClickCommandProperty = DependencyProperty.RegisterAttached("DoubleClickCommand", typeof(ICommand), typeof(MouseBehavior),
            new FrameworkPropertyMetadata(null, OnDoubleClickCommandChanged));

        public static readonly DependencyProperty LeftButtonDownCommandProperty = DependencyProperty.RegisterAttached("LeftButtonDownCommand", typeof(ICommand), typeof(MouseBehavior),
            new FrameworkPropertyMetadata(null, OnLeftButtonDownCommandChanged));

        public static readonly DependencyProperty LeftButtonUpCommandProperty = DependencyProperty.RegisterAttached("LeftButtonUpCommand", typeof(ICommand), typeof(MouseBehavior),
            new FrameworkPropertyMetadata(null, OnLeftButtonUpCommandChanged));

        public static readonly DependencyProperty PreviewLeftButtonDownCommandProperty = DependencyProperty.RegisterAttached("PreviewLeftButtonDownCommand", typeof(ICommand), typeof(MouseBehavior),
            new FrameworkPropertyMetadata(null, OnPreviewLeftButtonDownCommandChanged));

        public static readonly DependencyProperty PreviewLeftButtonUpCommandProperty = DependencyProperty.RegisterAttached("PreviewLeftButtonUpCommand", typeof(ICommand), typeof(MouseBehavior),
            new FrameworkPropertyMetadata(null, OnPreviewLeftButtonUpCommandChanged));

        public static object? GetCommandParameter(DependencyObject target) => target.GetValue(CommandParameterProperty);
        public static void SetCommandParameter(DependencyObject target, object? value) => target.SetValue(CommandParameterProperty, value);

        public static ICommand GetDoubleClickCommand(DependencyObject target) => (ICommand)target.GetValue(DoubleClickCommandProperty);
        public static void SetDoubleClickCommand(DependencyObject target, ICommand value) => target.SetValue(DoubleClickCommandProperty, value);

        public static ICommand GetLeftButtonDownCommand(DependencyObject target) => (ICommand)target.GetValue(LeftButtonDownCommandProperty);
        public static void SetLeftButtonDownCommand(DependencyObject target, ICommand value) => target.SetValue(LeftButtonDownCommandProperty, value);

        public static ICommand GetLeftButtonUpCommand(DependencyObject target) => (ICommand)target.GetValue(LeftButtonUpCommandProperty);
        public static void SetLeftButtonUpCommand(DependencyObject target, ICommand value) => target.SetValue(LeftButtonUpCommandProperty, value);

        public static ICommand GetPreviewLeftButtonDownCommand(DependencyObject target) => (ICommand)target.GetValue(PreviewLeftButtonDownCommandProperty);
        public static void SetPreviewLeftButtonDownCommand(DependencyObject target, ICommand value) => target.SetValue(PreviewLeftButtonDownCommandProperty, value);

        public static ICommand GetPreviewLeftButtonUpCommand(DependencyObject target) => (ICommand)target.GetValue(PreviewLeftButtonUpCommandProperty);
        public static void SetPreviewLeftButtonUpCommand(DependencyObject target, ICommand value) => target.SetValue(PreviewLeftButtonUpCommandProperty, value);

        private static void OnDoubleClickCommandChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            if (target is Control control)
            {
                if ((e.NewValue != null) && (e.OldValue == null))
                {
                    control.MouseDoubleClick += MouseDoubleClick;
                }
                else if ((e.NewValue == null) && (e.OldValue != null))
                {
                    control.MouseDoubleClick -= MouseDoubleClick;
                }
            }
        }

        private static void OnLeftButtonDownCommandChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            if (target is UIElement element)
            {
                if ((e.NewValue != null) && (e.OldValue == null))
                {
                    element.MouseLeftButtonDown += MouseLeftButtonDown;
                }
                else if ((e.NewValue == null) && (e.OldValue != null))
                {
                    element.MouseLeftButtonDown -= MouseLeftButtonDown;
                }
            }
        }

        private static void OnLeftButtonUpCommandChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            if (target is UIElement element)
            {
                if ((e.NewValue != null) && (e.OldValue == null))
                {
                    element.MouseLeftButtonUp += MouseLeftButtonUp;
                }
                else if ((e.NewValue == null) && (e.OldValue != null))
                {
                    element.MouseLeftButtonUp -= MouseLeftButtonUp;
                }
            }
        }

        private static void OnPreviewLeftButtonDownCommandChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            if (target is UIElement element)
            {
                if ((e.NewValue != null) && (e.OldValue == null))
                {
                    element.PreviewMouseLeftButtonDown += PreviewMouseLeftButtonDown;
                }
                else if ((e.NewValue == null) && (e.OldValue != null))
                {
                    element.PreviewMouseLeftButtonDown -= PreviewMouseLeftButtonDown;
                }
            }
        }

        private static void OnPreviewLeftButtonUpCommandChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            if (target is UIElement element)
            {
                if ((e.NewValue != null) && (e.OldValue == null))
                {
                    element.PreviewMouseLeftButtonUp += PreviewMouseLeftButtonUp;
                }
                else if ((e.NewValue == null) && (e.OldValue != null))
                {
                    element.PreviewMouseLeftButtonUp -= PreviewMouseLeftButtonUp;
                }
            }
        }

        private static void MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ICommand command = GetDoubleClickCommand((DependencyObject)sender);
            command.Execute(GetCommandParameter((DependencyObject)sender));
        }

        private static void MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ICommand command = GetLeftButtonDownCommand((DependencyObject)sender);
            command.Execute(GetCommandParameter((DependencyObject)sender));
        }

        private static void MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ICommand command = GetLeftButtonUpCommand((DependencyObject)sender);
            command.Execute(GetCommandParameter((DependencyObject)sender));
        }

        private static void PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ICommand command = GetPreviewLeftButtonDownCommand((DependencyObject)sender);
            command.Execute(GetCommandParameter((DependencyObject)sender));
        }

        private static void PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ICommand command = GetPreviewLeftButtonUpCommand((DependencyObject)sender);
            command.Execute(GetCommandParameter((DependencyObject)sender));
        }
    }
}
