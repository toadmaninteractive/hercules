using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;

namespace Hercules.Shell
{
    public abstract class UiOption
    {
    }

    public abstract class UiCustomOption : UiOption
    {
        public abstract string ToolbarCommandTemplateKey { get; }
        public abstract string ItemContainerTemplateKey { get; }
    }

    public sealed class UiSeparator : UiOption
    {
        private UiSeparator()
        {
        }

        public static readonly UiSeparator Instance = new();
    }

    public class UiCommandOption : UiOption
    {
        public string Text { get; }
        public string? Icon { get; }
        public string? IconImage => Icon == null ? null : "image-" + Icon;
        public ICommand Command { get; }

        public string? InputGestureText
        {
            get
            {
                if (Gesture == null)
                    return null;
                return string.IsNullOrEmpty(Gesture.DisplayString) ? Gesture.GetDisplayStringForCulture(CultureInfo.InvariantCulture) : Gesture.DisplayString;
            }
        }

        public KeyGesture? Gesture { get; set; }

        public UiCommandOption(string text, string? icon, ICommand command, KeyGesture? gesture = null)
        {
            Text = text;
            Icon = icon;
            Command = command;
            Gesture = gesture;
            if (gesture == null && command is RoutedCommand routedCommand && routedCommand.InputGestures != null)
            {
                Gesture = routedCommand.InputGestures.OfType<KeyGesture>().FirstOrDefault();
            }
        }

        public UiCommandOption(string text, string? icon, Action action, KeyGesture? gesture = null)
            : this(text, icon, Commands.Execute(action), gesture)
        {
        }
    }

    public class UiToggleOption : UiCommandOption
    {
        public IObservableValue<bool> Source { get; private set; }

        public UiToggleOption(string text, string? icon, IObservableValue<bool> source)
            : base(text, icon, new ToggleCommand(source))
        {
            Source = source;
        }
    }

    public class UiCategoryOption : UiOption
    {
        public string Name { get; }
        public ObservableCollection<UiOption> Items { get; } = new ObservableCollection<UiOption>();

        public UiCategoryOption(string name, IEnumerable<UiOption> items)
        {
            Name = name;
            Items.AddRange(items);
        }
    }
}
