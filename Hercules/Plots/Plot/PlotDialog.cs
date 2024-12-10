using Hercules.Forms.Schema.Custom;
using Hercules.Shell;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Hercules.Plots
{
    public class PlotPoint : NotifyPropertyChanged
    {
        Point position;

        public Point Position
        {
            get => position;
            set => SetField(ref position, value);
        }

        Brush? brush;

        public Brush? Brush
        {
            get => brush;
            set => SetField(ref brush, value);
        }

        public PlotDialog Dialog { get; }

        public ICommand DeleteCommand { get; }

        public PlotPoint(PlotDialog dialog, Point position)
        {
            Dialog = dialog;
            Position = position;
            DeleteCommand = Commands.Execute(() => dialog.DeletePoint(this));
        }
    }

    public class PlotDialog : Dialog
    {
        public ObservableCollection<PlotPoint> Points { get; } = new ObservableCollection<PlotPoint>();

        public Rect Viewport { get; }

        public ICommand ClearCommand { get; }
        public ICommand<Point> AddPointCommand { get; }

        public const int LerpPointsNumber = 10;
        public static readonly Color[] LerpColors = new[] { Colors.Gold, Colors.Orange, Colors.Red, Colors.BlueViolet, Colors.RoyalBlue, Colors.Blue, Colors.Brown, Colors.Green };

        public EditorPlotData Result => new EditorPlotData(this.Points.Select(p => p.Position).ToList());

        public PlotDialog(EditorPlot editor, Optional<EditorPlotData> data)
        {
            Viewport = editor.DefaultViewport;
            ClearCommand = Commands.Execute(() => Points.Clear());
            AddPointCommand = Commands.Execute<Point>(AddPoint);
            if (data.HasValue)
            {
                Points.AddRange(data.Value!.Points.Select(p => new PlotPoint(this, p)));
                Recolor();
            }
        }

        void AddPoint(Point position)
        {
            Points.Add(new PlotPoint(this, position));
            Recolor();
        }

        public void DeletePoint(PlotPoint point)
        {
            Points.Remove(point);
            Recolor();
        }

        void Recolor()
        {
            var i = 0;
            foreach (var point in Points)
            {
                int ii = i / LerpPointsNumber;
                float t = (i % LerpPointsNumber) / (float)LerpPointsNumber;
                var fromColor = ii < LerpColors.Length ? LerpColors[ii] : Colors.Black;
                var toColor = ii + 1 < LerpColors.Length ? LerpColors[ii + 1] : Colors.Black;
                point.Brush = new SolidColorBrush(Color.Add(Color.Multiply(fromColor, 1 - t), Color.Multiply(toColor, t)));
                i++;
            }
        }
    }
}