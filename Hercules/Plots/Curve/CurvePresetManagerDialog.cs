using Hercules.Forms.Schema.Custom;
using Hercules.Shell;
using System.Collections.Generic;
using System.Windows;

namespace Hercules.Plots
{
    public class CurvePresetManagerDialog : Dialog
    {
        public IReadOnlyList<EditorCurvePreset> Presets { get; }
        public EditorCurvePreset? SelectedPreset
        {
            get;
            set
            {
                if (SetField(ref field, value))
                {
                    if (IsSaveMode)
                        SelectedName = SelectedPreset?.Name;
                }
            }
        }

        public string? SelectedName
        {
            get;
            set => SetField(ref field, value);
        }

        public bool IsSaveMode { get; }

        public Rect Viewport { get; }

        protected override bool IsOkEnabled()
        {
            if (IsSaveMode)
                return !string.IsNullOrWhiteSpace(SelectedName);
            else
                return SelectedPreset != null;
        }

        public CurvePresetManagerDialog(IReadOnlyList<EditorCurvePreset> presets, Rect viewport, bool isSaveMode)
        {
            Presets = presets;
            Viewport = viewport;
            IsSaveMode = isSaveMode;
        }
    }
}