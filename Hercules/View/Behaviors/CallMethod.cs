using Microsoft.Xaml.Behaviors;
using System;
using System.Reflection;
using System.Windows;

namespace Hercules.Controls
{
    public class CallMethod : TargetedTriggerAction<DependencyObject>
    {
        public static readonly DependencyProperty MethodNameProperty = DependencyProperty.Register("MethodName", typeof(string), typeof(CallMethod), new FrameworkPropertyMetadata(OnMethodNameChanged));

        private MethodInfo? methodInfo;

        public string MethodName
        {
            get => (string)this.GetValue(MethodNameProperty);
            set => this.SetValue(MethodNameProperty, value);
        }

        private static void OnMethodNameChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            CallMethod callMethodAction = (CallMethod)sender;
            callMethodAction.UpdateMethodInfo();
        }

        protected override void Invoke(object parameter)
        {
            if (this.methodInfo != null)
            {
                var parameters = this.methodInfo.GetParameters();
                if (parameters.Length == 0)
                    this.methodInfo.Invoke(this.Target, null);
                else if (parameters.Length == 1)
                    this.methodInfo.Invoke(this.Target, [parameter]);
                else if (parameters.Length == 2)
                    this.methodInfo.Invoke(this.Target, [AssociatedObject, parameter]);
            }
        }

        protected override void OnTargetChanged(DependencyObject oldTarget, DependencyObject newTarget)
        {
            base.OnTargetChanged(oldTarget, newTarget);
            this.UpdateMethodInfo();
        }

        private void UpdateMethodInfo()
        {
            if (this.Target != null && !string.IsNullOrEmpty(this.MethodName))
            {
                Type targetType = this.Target.GetType();
                MethodInfo? newMethodInfo = targetType.GetMethod(this.MethodName);
                if (newMethodInfo == null)
                    throw new ArgumentException("Method " + this.MethodName + " doesn't exist in type " + targetType.Name);
                this.methodInfo = newMethodInfo;
            }
            else
            {
                this.methodInfo = null;
            }
        }
    }
}
