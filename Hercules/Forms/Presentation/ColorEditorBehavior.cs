using Hercules.Forms.Elements;
using Hercules.Forms.Schema;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Hercules.Forms.Presentation
{
    public class ColorEditorBehavior : VirtualRowItemBehavior
    {
        public RecordElement Element { get; }
        public ColorRecordSchema ColorSchema { get; }
        public Color ColorValue
        {
            get
            {
                Color color = Colors.Black;
                foreach (var f in Element.Children)
                {
                    if (f.SchemaField == ColorSchema.Red)
                        color.R = GetColorChannel(f);
                    if (f.SchemaField == ColorSchema.Green)
                        color.G = GetColorChannel(f);
                    if (f.SchemaField == ColorSchema.Blue)
                        color.B = GetColorChannel(f);
                    if (f.SchemaField == ColorSchema.Alpha)
                        color.A = GetColorChannel(f);
                }
                return color;
            }
            set
            {
                Element.Form.Run(transaction =>
                {
                    setterTransactionId = transaction.TransactionId;
                    foreach (var f in Element.Children)
                    {
                        if (f.SchemaField == ColorSchema.Red)
                            SetColorChannel(f, value.R, transaction);
                        if (f.SchemaField == ColorSchema.Green)
                            SetColorChannel(f, value.G, transaction);
                        if (f.SchemaField == ColorSchema.Blue)
                            SetColorChannel(f, value.B, transaction);
                        if (f.SchemaField == ColorSchema.Alpha)
                            SetColorChannel(f, value.A, transaction);
                    }
                });
            }
        }

        private byte GetColorChannel(Field field)
        {
            return field.DeepElement switch
            {
                IntElement ie => (byte)(ie.Value ?? 0),
                FloatElement fe => (byte)(int)((fe.Value ?? 0) * 255.0),
                _ => 0
            };
        }

        private void SetColorChannel(Field field, byte channel, ITransaction transaction)
        {
            switch (field.DeepElement)
            {
                case IntElement ie:
                    ie.SetValue(channel, transaction);
                    break;
                case FloatElement fe:
                    fe.SetValue(channel / 255.0, transaction);
                    break;
            }
        }

        public static readonly DependencyProperty ColorProperty = DependencyProperty.RegisterAttached("Color", typeof(Color), typeof(ColorEditorBehavior), new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.Inherits));
        public static Color GetColor(DependencyObject d) => (Color)d.GetValue(ColorProperty);
        public static void SetColor(DependencyObject d, Color value) => d.SetValue(ColorProperty, value);

        public static readonly DependencyProperty ToggleDropDownCommandProperty = DependencyProperty.RegisterAttached(nameof(ToggleDropDownCommand), typeof(ICommand), typeof(ColorEditorBehavior), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));
        public static ICommand? GetToggleDropDownCommand(DependencyObject d) => (ICommand)d.GetValue(ToggleDropDownCommandProperty);
        public static void SetToggleDropDownCommand(DependencyObject d, ICommand? value) => d.SetValue(ToggleDropDownCommandProperty, value);

        public static readonly DependencyProperty SubmitCommandProperty = DependencyProperty.RegisterAttached(nameof(SubmitCommand), typeof(ICommand), typeof(ColorEditorBehavior), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));
        public static ICommand? GetSubmitCommand(DependencyObject d) => (ICommand)d.GetValue(SubmitCommandProperty);
        public static void SetSubmitCommand(DependencyObject d, ICommand? value) => d.SetValue(SubmitCommandProperty, value);

        public ColorEditorBehavior(VirtualRowItem item, RecordElement element, ColorRecordSchema colorSchema) : base(item)
        {
            Element = element;
            ColorSchema = colorSchema;
            ToggleDropDownCommand = Commands.Execute(ToggleDropDown);
            SubmitCommand = Commands.Execute<Color>(Submit);
            element.Events.OnValueChanged += Events_OnValueChanged;
        }

        private void Submit(Color color)
        {
            ColorValue = color;
            if (Item.Popup != null)
                Item.Popup.IsOpen = false;
            if (Item.View != null)
                SetColor(Item.View, color);
        }

        private void Events_OnValueChanged(Element element, ITransaction transaction)
        {
            if (Item.View != null && transaction.TransactionId != setterTransactionId)
                transaction.OnPostUpdate += () => SetColor(Item.View, ColorValue);
        }

        public ICommand ToggleDropDownCommand { get; }
        public ICommand SubmitCommand { get; }
        private int setterTransactionId = -1;

        public override void OnCreateVisual(FrameworkElement view)
        {
            SetColor(view, ColorValue);
            SetToggleDropDownCommand(view, ToggleDropDownCommand);
            SetSubmitCommand(view, SubmitCommand);
        }

        public override void OnDisposeVisual(FrameworkElement view)
        {
            view.ClearValue(ColorProperty);
            view.ClearValue(ToggleDropDownCommandProperty);
            view.ClearValue(SubmitCommandProperty);
        }

        private void ToggleDropDown()
        {
            var popup = Item.CreatePopup(false);
            popup.IsOpen = !popup.IsOpen;
        }
    }
}
