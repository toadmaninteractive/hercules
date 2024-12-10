using Hercules.Forms.Elements;
using Hercules.Forms.Presentation;
using Hercules.Forms.Schema.Custom;
using Json;
using System;
using System.Linq;
using System.Windows;

namespace Hercules.Plots
{
    public class CurveElement : EditableCustomProxy<CurveSchemaType>
    {
        private static readonly EditorCurveData emptyCurve = new EditorCurveData(Array.Empty<EditorCurveKnot>());

        private EditorCurveData curveData;
        public EditorCurveData CurveData
        {
            get => curveData;
            set => SetField(ref curveData, value);
        }

        Rect viewport;
        public Rect Viewport
        {
            get => viewport;
            set => SetField(ref viewport, value);
        }

        private readonly EditorCurve editor;

        public EditorCurve Editor => editor;

        public CurveElement(IContainer parent, CurveSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, type, json, originalJson, transaction)
        {
            curveData = EditorCurveDataJsonSerializer.Instance.TryDeserialize(json).GetValueOrDefault(emptyCurve);
            editor = CustomType.GetEditor();
            viewport = editor.DefaultViewport;
            AdjustViewport();
            HasPostUpdate = true;
        }

        protected override void PostUpdate()
        {
            CurveData = EditorCurveDataJsonSerializer.Instance.TryDeserialize(Json).GetValueOrDefault(emptyCurve);
            AdjustViewport();
        }

        private void AdjustViewport()
        {
            if (editor.AutoScalePreviewX || editor.AutoScalePreviewY)
            {
                var newViewport = editor.DefaultViewport;
                var bounds = CurveData.Knots.Select(p => p.Position).GetBounds();
                if (bounds.Width > 0 && editor.AutoScalePreviewX)
                {
                    newViewport.X = bounds.X;
                    newViewport.Width = bounds.Width;
                }
                if (bounds.Height > 0 && editor.AutoScalePreviewY)
                {
                    newViewport.Y = bounds.Y;
                    newViewport.Height = bounds.Height;
                }
                Viewport = newViewport;
            }
        }

        public override void Present(PresentationContext context)
        {
            var proxy = context.GetProxy(this);
            var left = context.Left;
            context.AddItem(proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool("CurveElement"), width: editor.PreviewWidth), height: editor.PreviewHeight);
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

        protected override void Edit()
        {
            var originalJson = Json;
            var value = EditorCurveDataJsonSerializer.Instance.TryDeserialize(originalJson);
            var editorObject = editor;
            var originalPressetCount = editorObject.Presets?.Count ?? 0;
            var dialog = new CurveDialog($"Curve Editor: {Path}", editorObject, value, CustomType.DialogService, CustomType.SaveEditor);

            if (CustomType.DialogService.ShowDialog(dialog))
            {
                var result = EditorCurveDataJsonSerializer.Instance.Serialize(dialog.Result);
                if (!originalJson.Equals(result))
                    Form.Run(t => SetJson(result, t), this);

                if (originalPressetCount != (editorObject.Presets?.Count ?? 0))
                    CustomType.OpenEditorInNewTab(editorObject);
            }
        }
    }
}