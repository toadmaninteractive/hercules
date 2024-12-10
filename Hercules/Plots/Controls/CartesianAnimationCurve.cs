using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Hercules.Controls
{
    public class CartesianAnimationCurve : CartesianPlot
    {
        protected override CartesianControl CreateKnotView(object viewModel, Style knotStyle)
        {
            return new CartesianAnimationCurveKnot(this) { DataContext = viewModel, Style = KnotStyle };
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (Elements.Count < 2)
                return;
            var pathFigure = new PathFigure { IsClosed = false, IsFilled = false };
            var geometry = new PathGeometry(new[] { pathFigure });
            var panel = (CartesianPanel)VisualParent;
            Point? lastPosition = null;
            double? lastTangent = null;
            foreach (var element in Elements.OrderBy(e => e.View.Position.X))
            {
                var view = (CartesianAnimationCurveKnot)element.View;
                var pos = element.View.Position;
                if (lastPosition == null)
                {
                    pathFigure.StartPoint = panel.ModelToRender(pos);
                }
                else
                {
                    var tangent1 = lastTangent!.Value;
                    var tangent2 = view.TangentIn;
                    var dx = pos.X - lastPosition.Value.X;
                    var vTangent1 = new Vector(1, tangent1);
                    vTangent1 *= dx;
                    var vTangent2 = new Vector(1, -tangent2);
                    vTangent2 *= dx;
                    const double oneThird = 1.0 / 3.0;
                    var p1 = lastPosition.Value + oneThird * vTangent1;
                    var p2 = pos - oneThird * vTangent2;
                    var segment = new BezierSegment(panel.ModelToRender(p1), panel.ModelToRender(p2), panel.ModelToRender(pos), true) { IsSmoothJoin = false };
                    pathFigure.Segments.Add(segment);
                }
                lastPosition = element.View.Position;
                lastTangent = view.TangentOut;
            }
            drawingContext.DrawGeometry(null, GetStrokePen(), geometry);
        }
    }
}
