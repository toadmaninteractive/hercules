using System.Windows;
using System.Windows.Input;

namespace Hercules.Controls
{
    public class PanGesture
    {
        private bool isMouseDown;
        private bool started;
        private Point previousPosition;
        private readonly VirtualCanvas owner;

        /// <summary>
        /// Threashold used to know if we can start the movement of the selection or not. We start the movement when the distance
        /// between the previousPosition and the current mouse position is more than the threshold
        /// </summary>
        public const double Threshold = 10;

        public PanGesture(VirtualCanvas owner)
        {
            this.owner = owner;
            owner.PreviewMouseDown += OnMouseDown;
            owner.PreviewMouseMove += OnMouseMove;
            owner.PreviewMouseUp += OnMouseUp;
            owner.LostMouseCapture += OnLostMouseCapture;
        }

        private void OnLostMouseCapture(object sender, MouseEventArgs e)
        {
            FinishPanning();
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            FinishPanning();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (isMouseDown)
            {
                var position = e.GetPosition(this.owner);
                if (!started)
                {
                    Vector deplacement = position - this.previousPosition;
                    if (deplacement.Length > Threshold)
                    {
                        started = true;
                        Mouse.Capture(owner);
                    }
                }
                else
                {
                    // request to move only if needed, in order to save a few CPU cylces.
                    if (position != previousPosition)
                    {
                        // double dx = position.X - this.previousPosition.X;
                        double dy = position.Y - this.previousPosition.Y;
                        this.owner.ScrollBy(dy);
                        this.previousPosition = position;
                        owner.Cursor = Cursors.ScrollNS;
                    }
                }
            }
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && (ReferenceEquals(e.OriginalSource, owner.ContentCanvas) || ReferenceEquals(e.OriginalSource, owner)))
            {
                this.isMouseDown = true;
                this.started = false;
                this.previousPosition = e.GetPosition(this.owner);
            }
        }

        private void FinishPanning()
        {
            this.isMouseDown = false;
            this.owner.ReleaseMouseCapture();
            owner.Cursor = Cursors.Arrow;
        }
    }
}
