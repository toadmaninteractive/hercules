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
            get => selectedPreset;
            set
            {
                if (SetField(ref selectedPreset, value))
                {
                    if (IsSaveMode)
                        SelectedName = SelectedPreset?.Name;
                }
            }
        }

        string? selectedName;
        public string? SelectedName
        {
            get => selectedName;
            set => SetField(ref selectedName, value);
        }

        public bool IsSaveMode { get; }

        public Rect Viewport { get; }

        private EditorCurvePreset? selectedPreset;

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