using Hercules.Forms.Schema;
using System.Windows;
using System.Windows.Media;
using Telerik.Windows.Controls.Diagrams;

namespace Hercules.Diagrams
{
    public abstract class BaseConnector : RadDiagramConnector
    {
        public ConnectorKind Kind { get; }
        public string? Category { get; }
        public BlockBase BlockViewModel { get; }

        protected Color BaseFillColor { get; }

        protected BaseConnector(BlockBase block, SchemaConnector schema, Visibility labelVisibility)
        {
            BlockViewModel = block;
            Kind = schema.Kind;
            Category = schema.Category;
            Name = schema.Name;
            Color = new SolidColorBrush(schema.Color);
            Caption = schema.Caption;
            Offset = schema.Position;
            LabelVisibility = labelVisibility;
            BaseFillColor = (Color)ColorConverter.ConvertFromString("#FFE1EEFC");
            FillColor = new SolidColorBrush(BaseFillColor);
        }

        public static readonly DependencyProperty LabelVisibilityProperty =
            DependencyProperty.Register("LabelVisibility", typeof(Visibility), typeof(BaseConnector));

        public static readonly DependencyProperty CaptionProperty =
            DependencyProperty.Register("Caption", typeof(string), typeof(BaseConnector));

        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(SolidColorBrush), typeof(BaseConnector));

        public static readonly DependencyProperty FillColorProperty =
            DependencyProperty.Register("FillColor", typeof(SolidColorBrush), typeof(BaseConnector));

        public Visibility LabelVisibility
        {
            get => (Visibility)GetValue(LabelVisibilityProperty);
            set => SetValue(LabelVisibilityProperty, value);
        }

        public string Caption
        {
            get => (string)GetValue(CaptionProperty);
            set => SetValue(CaptionProperty, value);
        }

        public SolidColorBrush Color
        {
            get => (SolidColorBrush)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        public SolidColorBrush FillColor
        {
            get => (SolidColorBrush)GetValue(FillColorProperty);
            set => SetValue(FillColorProperty, value);
        }

        public override string ToString()
        {
            return $"Offset: {Offset}; Name: {Name}; Kind: {Kind}";
        }
    }
}