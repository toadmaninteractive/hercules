using Hercules.Forms.Elements;
using Hercules.Forms.Presentation;
using Hercules.Forms.Schema.Custom;
using Json;
using System;
using System.Windows;

namespace Hercules.Plots
{
    public class PlotElement : EditableCustomProxy<PlotSchemaType>
    {
        private static readonly EditorPlotData emptyPlot = new EditorPlotData(Array.Empty<Point>());

        private EditorPlotData plotData;
        public EditorPlotData PlotData
        {
            get => plotData;
            private set => SetField(ref plotData, value);
        }

        Rect viewport;
        public Rect Viewport
        {
            get => viewport;
            private set => SetField(ref viewport, value);
        }

        public EditorPlot Editor { get; }

        public PlotElement(IContainer parent, PlotSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, type, json, originalJson, transaction)
        {
            plotData = EditorPlotDataJsonSerializer.Instance.TryDeserialize(json).GetValueOrDefault(emptyPlot);
            Editor = CustomType.GetEditor();
            viewport = Editor.DefaultViewport;
            AdjustViewport();
            HasPostUpdate = true;
        }

        protected override void PostUpdate()
        {
            PlotData = EditorPlotDataJsonSerializer.Instance.TryDeserialize(Json).GetValueOrDefault(emptyPlot);
            AdjustViewport();
        }

        private void AdjustViewport()
        {
            if (Editor.AutoScalePreviewX || Editor.AutoScalePreviewY)
            {
                var newViewport = Editor.DefaultViewport;
                var bounds = PlotData.Points.GetBounds();
                if (bounds.Width > 0 && Editor.AutoScalePreviewX)
                {
                    newViewport.X = bounds.X;
                    newViewport.Width = bounds.Width;
                }
                if (bounds.Height > 0 && Editor.AutoScalePreviewY)
                {
                    newViewport.Y = bounds.Y;
                    newViewport.Height = bounds.Height;
                }
                Viewport = newViewport;
            }
        }

        protected override void Edit()
        {
            var originalJson = Json;
            var value = EditorPlotDataJsonSerializer.Instance.TryDeserialize(originalJson);
            var dialog = new PlotDialog(Editor, value);
            if (CustomType.DialogService.ShowDialog(dialog))
            {
                var result = EditorPlotDataJsonSerializer.Instance.Serialize(dialog.Result);
                if (!originalJson.Equals(result))
                {
                    Form.Run(t => SetJson(result, t), this);
                }
            }
        }

        public override void Present(PresentationContext context)
        {
            var proxy = context.GetProxy(this);
            var left = context.Left;
            context.AddItem(proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool("PlotElement"), width: Editor.PreviewWidth), height: Editor.PreviewHeight);
            if (!context.IsPropertyEditor)
                context.Indent(left);
            context.AddRow(proxy);
            context.AddItem(proxy.Item1 ??= new VirtualRowItem(this, ControlPools.GetPool("EditableProxy"), width: 100));
            if (IsExpanded)
            {
                context.AddRow(proxy, 1);
                Element.Present(context);
            }
            if (!context.IsPropertyEditor)
                context.Outdent();
        }
    }
}