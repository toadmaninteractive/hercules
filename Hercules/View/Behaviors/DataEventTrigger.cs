using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows;

namespace Hercules.Controls
{
    /// <summary>
    /// Trigger which fires when a CLR event is raised on an object.
    /// Can be used to trigger from events on the data context, as opposed to
    /// a standard EventTrigger which uses routed events on FrameworkElements.
    /// </summary>
    public class DataEventTrigger : TriggerBase<FrameworkElement>
    {
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(object), typeof(DataEventTrigger), new PropertyMetadata(null, DataEventTrigger.HandleSourceChanged));
        public static readonly DependencyProperty EventNameProperty = DependencyProperty.Register("EventName", typeof(string), typeof(DataEventTrigger), new PropertyMetadata(null, DataEventTrigger.HandleEventNameChanged));

        private EventInfo? currentEvent;
        private Delegate? currentDelegate;
        private object? currentTarget;

        public object Source
        {
            get => GetValue(DataEventTrigger.SourceProperty);
            set => SetValue(DataEventTrigger.SourceProperty, value);
        }

        public string EventName
        {
            get => (string)this.GetValue(DataEventTrigger.EventNameProperty);
            set => this.SetValue(DataEventTrigger.EventNameProperty, value);
        }

        protected override void OnAttached()
        {
            UpdateHandler();
        }

        protected override void OnDetaching()
        {
            RemoveHandler();
        }

        private static void HandleEventNameChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((DataEventTrigger)sender).OnEventNameChanged(e);
        }

        protected virtual void OnEventNameChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateHandler();
        }

        private static void HandleSourceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((DataEventTrigger)sender).OnSourceChanged(e);
        }

        protected virtual void OnSourceChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateHandler();
        }

        void RemoveHandler()
        {
            if (this.currentEvent != null)
            {
                this.currentEvent.RemoveEventHandler(this.currentTarget, this.currentDelegate);

                this.currentEvent = null;
                this.currentTarget = null;
                this.currentDelegate = null;
            }
        }

        private void UpdateHandler()
        {
            RemoveHandler();

            this.currentTarget = this.Source;

            if (this.currentTarget != null && !string.IsNullOrEmpty(this.EventName))
            {
                Type targetType = this.currentTarget.GetType();
                this.currentEvent = targetType.GetEvent(this.EventName);
                if (this.currentEvent != null)
                {
                    this.currentDelegate = this.GetDelegate(this.currentEvent);
                    this.currentEvent.AddEventHandler(this.currentTarget, this.currentDelegate);
                }
            }
        }

        private Delegate GetDelegate(EventInfo eventInfo)
        {
            if (typeof(EventHandler).IsAssignableFrom(eventInfo.EventHandlerType))
            {
                MethodInfo method = this.GetType().GetMethod(nameof(OnEvent), BindingFlags.NonPublic | BindingFlags.Instance)!;
                return Delegate.CreateDelegate(eventInfo.EventHandlerType, this, method);
            }

            Type handlerType = eventInfo.EventHandlerType;
            ParameterInfo[] eventParams = handlerType.GetMethod("Invoke").GetParameters();

            if (eventParams.Length == 1)
            {
                Action<object> action = OnMethodWithParameter;
                ParameterExpression parameter = System.Linq.Expressions.Expression.Parameter(eventParams[0].ParameterType, "x");
                var parameterAsObject = System.Linq.Expressions.Expression.Convert(parameter, typeof(object));

                MethodCallExpression methodExpression = System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression.Constant(action), action.GetType().GetMethod("Invoke"), parameterAsObject);
                LambdaExpression lambdaExpression = System.Linq.Expressions.Expression.Lambda(methodExpression, parameter);
                return Delegate.CreateDelegate(handlerType, lambdaExpression.Compile(), "Invoke", false);
            }
            else
            {
                Action action = OnMethod;
                IEnumerable<ParameterExpression> parameters = eventParams.Select(p => System.Linq.Expressions.Expression.Parameter(p.ParameterType, "x"));

                MethodCallExpression methodExpression = System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression.Constant(action), action.GetType().GetMethod("Invoke"));
                LambdaExpression lambdaExpression = System.Linq.Expressions.Expression.Lambda(methodExpression, parameters);
                return Delegate.CreateDelegate(handlerType, lambdaExpression.Compile(), "Invoke", false);
            }
        }

        private void OnMethod()
        {
            this.InvokeActions(null);
        }

        private void OnMethodWithParameter(object parameter)
        {
            this.InvokeActions(parameter);
        }

        private void OnEvent(object sender, EventArgs e)
        {
            this.InvokeActions(e);
        }
    }
}
