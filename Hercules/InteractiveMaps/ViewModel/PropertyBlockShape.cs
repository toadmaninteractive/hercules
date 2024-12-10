using Hercules.Forms.Elements;
using Json;
using System;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Media;
using Telerik.Windows.Controls;

namespace Hercules.InteractiveMaps
{
    public class PropertyBlockShape : RadDiagramShape
    {
        public ListItem FormListItem { get; private set; }
        public string RefId => elRefId.Value;

        private DocumentForm form;
        private FloatElement posLeft = default!;
        private FloatElement posTop = default!;
        private FloatElement posRight = default!;
        private FloatElement posBottom = default!;
        private IntElement elZIndex = default!;
        private StringElement elRefId = default!;

        private IDisposable blockSubscription;
        private readonly object layoutUndoRedoGroup = new object();
        private bool isSettingPositionFromForm;
        private bool isSettingPositionFromDiagram;

        private double scalingFactorX = 1.0;
        private double scalingFactorY = 1.0;

        private double unscaledLeft = 0.0;
        private double unscaledTop = 0.0;
        private double unscaledWidth = 0.0;
        private double unscaledHeight = 0.0;

        private readonly Color validBackground = (Color)ColorConverter.ConvertFromString("#9F66CDAA");
        private readonly Color validForeground = (Color)ColorConverter.ConvertFromString("#FF2E8B57");
        private readonly Color invalidBackground = (Color)ColorConverter.ConvertFromString("#7FFA8072");
        private readonly Color invalidForeground = (Color)ColorConverter.ConvertFromString("#FFFA8072");
        private readonly Color undefinedBackground = (Color)ColorConverter.ConvertFromString("#7F708090");
        private readonly Color undefinedForeground = (Color)ColorConverter.ConvertFromString("#FF708090");

        public PropertyBlockShape()
        {
            Background = new SolidColorBrush(undefinedBackground);
            BorderBrush = new SolidColorBrush(undefinedForeground);
            BorderThickness = new Thickness(.25);
            FontFamily = new FontFamily("Trebuchet MS");
            FontWeight = FontWeights.UltraLight;
            FontSize = 20;
            IsConnectorsManipulationEnabled = false;
            IsRotationEnabled = false;
        }

        public void SetFields(ListItem formItem)
        {
            FormListItem = formItem;
            form = formItem.Form;

            posLeft = (FloatElement)FormListItem.GetByPath(new JsonPath("pos", "left"));
            posTop = (FloatElement)FormListItem.GetByPath(new JsonPath("pos", "top"));
            posRight = (FloatElement)FormListItem.GetByPath(new JsonPath("pos", "right"));
            posBottom = (FloatElement)FormListItem.GetByPath(new JsonPath("pos", "bottom"));
            elZIndex = (IntElement)FormListItem.GetByPath(new JsonPath("z_index"));
            elRefId = (StringElement)FormListItem.GetByPath(new JsonPath("ref_id"));

            /*
            SetPositionFromForm();
            SetZIndexFromForm();
            */

            SetRefIdFromForm(elRefId.Value);

            blockSubscription = new CompositeDisposable(
                posLeft.OnPropertyChanged(nameof(FloatElement.Value)).Subscribe(_ => SetPositionFromForm()),
                posTop.OnPropertyChanged(nameof(FloatElement.Value)).Subscribe(_ => SetPositionFromForm()),
                posRight.OnPropertyChanged(nameof(FloatElement.Value)).Subscribe(_ => SetPositionFromForm()),
                posBottom.OnPropertyChanged(nameof(FloatElement.Value)).Subscribe(_ => SetPositionFromForm()),
                elZIndex.OnPropertyChanged(nameof(FloatElement.Value)).Subscribe(_ => SetZIndexFromForm()),
                elRefId.OnPropertyChanged(nameof(StringElement.Value), s => s.Value).Subscribe(SetRefIdFromForm)
            );
        }

        void SetPositionFromForm()
        {
            if (isSettingPositionFromDiagram)
                return;

            SetPositionFromFormNoCheck();
        }

        void SetPositionFromFormNoCheck()
        {
            isSettingPositionFromForm = true;

            unscaledLeft = (posLeft.Value ?? 0) * scalingFactorX;
            unscaledTop = (posTop.Value ?? 0) * scalingFactorY;
            unscaledWidth = ((posRight.Value ?? 0) - (posLeft.Value ?? 0)) * scalingFactorX;
            unscaledHeight = ((posBottom.Value ?? 0) - (posTop.Value ?? 0)) * scalingFactorY;

            Position = new Point(unscaledLeft, unscaledTop);
            Width = unscaledWidth;
            Height = unscaledHeight;
            isSettingPositionFromForm = false;
        }

        void SetZIndexFromForm()
        {
            ZIndex = elZIndex.Value ?? 0;
        }

        void SetRefIdFromForm(string? ref_id)
        {
            var isValid = !string.IsNullOrEmpty(ref_id?.Trim() ?? null);
            Background = new SolidColorBrush(isValid ? validBackground : invalidBackground);
            BorderBrush = new SolidColorBrush(isValid ? validForeground : invalidForeground);
        }

        protected override void OnPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(Bounds) && !isSettingPositionFromForm)
            {
                isSettingPositionFromDiagram = true;

                form.Run(transaction =>
                {
                    if (Position.X != unscaledLeft)
                        posLeft.SetValue(Position.X / scalingFactorX, transaction, layoutUndoRedoGroup);

                    if (Position.Y != unscaledTop)
                        posTop.SetValue(Position.Y / scalingFactorY, transaction, layoutUndoRedoGroup);

                    if (Position.X != unscaledLeft || Width != unscaledWidth)
                        posRight.SetValue((Position.X + Width) / scalingFactorX, transaction, layoutUndoRedoGroup);

                    if (Position.Y != unscaledTop || Height != unscaledHeight)
                        posBottom.SetValue((Position.Y + Height) / scalingFactorY, transaction, layoutUndoRedoGroup);
                });

                isSettingPositionFromDiagram = false;
            }

            base.OnPropertyChanged(propertyName);
        }

        public void OnRemove()
        {
            if (blockSubscription != null)
                blockSubscription.Dispose();
        }

        public void UpdateScalingFactors(double diagramWidth, double diagramHeight)
        {
            scalingFactorX = diagramWidth;
            scalingFactorY = diagramHeight;
            SetPositionFromForm();
        }
    }
}
