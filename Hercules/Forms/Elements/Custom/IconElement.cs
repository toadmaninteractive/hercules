using Hercules.Forms.Presentation;
using Hercules.Forms.Schema;
using Hercules.Shell;
using Json;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Hercules.Forms.Elements
{
    public class IconElementDialog : Dialog
    {
        public BitmapSource Image { get; }
        public ICommand<Point> SelectCommand { get; }

        public IconElementDialog(BitmapSource image, ICommand<Point> selectCommand)
        {
            this.Image = image;
            this.SelectCommand = selectCommand;
        }
    }

    public class IconElement : CustomProxy<IconSchemaType>
    {
        public IconElement(IContainer parent, IconSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, type, json, originalJson, transaction)
        {
            ShowPopupCommand = Commands.Execute(() => type.DialogService.ShowDialog(new IconElementDialog(type.Image, Commands.Execute<Point>(SetPoint))));
            value = ContentElement.Value;
            HasPostUpdate = true;
        }

        int? value;

        public ICommand ShowPopupCommand { get; }

        public IntElement ContentElement => (IntElement)DeepElement;

        public Int32Rect SourceRect
        {
            get
            {
                var width = CustomType.Image.PixelWidth / CustomType.Editor.IconWidth;
                var y = ((value ?? CustomType.Editor.Default) / width) * CustomType.Editor.IconHeight;
                var x = ((value ?? CustomType.Editor.Default) % width) * CustomType.Editor.IconWidth;
                return new Int32Rect(x, y, CustomType.Editor.IconWidth, CustomType.Editor.IconHeight);
            }
        }

        public CroppedBitmap Image => new CroppedBitmap(CustomType.Image, SourceRect);

        protected override void PostUpdate()
        {
            if (value != ContentElement.Value)
            {
                value = ContentElement.Value;
                RaisePropertyChanged(nameof(Image));
            }
        }

        public void SetPoint(Point point)
        {
            var x = (int)Math.Round(Math.Floor(point.X / CustomType.Editor.IconWidth));
            var y = (int)Math.Round(Math.Floor(point.Y / CustomType.Editor.IconHeight));
            var width = CustomType.Image.PixelWidth / CustomType.Editor.IconWidth;
            ContentElement.Value = x + y * width;
        }

        public override void Present(PresentationContext context)
        {
            var proxy = context.GetProxy(this);
            var left = context.Left;
            Element.Present(context);
            if (!context.IsPropertyEditor)
                context.Indent(left);
            context.AddRow(proxy);
            context.AddItem(proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool(GetType())), height: CustomType.IconHeight);
            if (!context.IsPropertyEditor)
                context.Outdent();
        }
    }
}
