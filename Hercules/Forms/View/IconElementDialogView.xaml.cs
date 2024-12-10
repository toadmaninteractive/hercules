using Hercules.Forms.Elements;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Hercules.Forms.View
{
    [ViewModelType(typeof(IconElementDialog))]
    public partial class IconElementDialogView : Window
    {
        public IconElementDialogView()
        {
            InitializeComponent();
        }

        Point mousePos;

        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var mousePos2 = e.GetPosition(Image);
            if (Math.Abs(mousePos.X - mousePos2.X) < 10 && Math.Abs(mousePos.Y - mousePos2.Y) < 10)
            {
                var form = (IconElementDialog)DataContext;
                form.SelectCommand.Execute(e.GetPosition(sender as Image));
                Close();
            }
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mousePos = e.GetPosition(Image);
        }

        private void Window_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var form = (IconElementDialog)DataContext;
            if (form != null)
            {
                var horizontalBorderHeight = SystemParameters.ResizeFrameHorizontalBorderHeight;
                var verticalBorderWidth = SystemParameters.ResizeFrameVerticalBorderWidth;
                var captionHeight = SystemParameters.CaptionHeight;

                var goodWidth = form.Image.PixelWidth + verticalBorderWidth * 2 + 40;
                var goodHeight = form.Image.PixelHeight + horizontalBorderHeight * 2 + captionHeight + 40;

                Width = Math.Min(goodWidth, SystemParameters.PrimaryScreenWidth * 0.7);
                Height = Math.Min(goodHeight, SystemParameters.PrimaryScreenHeight * 0.7);

                if (goodWidth > SystemParameters.PrimaryScreenWidth * 0.7 && goodHeight > SystemParameters.PrimaryScreenHeight * 0.7)
                    WindowState = WindowState.Maximized;
            }
        }
    }
}
