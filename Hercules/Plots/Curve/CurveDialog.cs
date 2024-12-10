using Hercules.Forms.Schema.Custom;
using Hercules.Shell;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Hercules.Plots
{
    public class CurveKnot : NotifyPropertyChanged
    {
        Point position;
        double tangentIn;
        double tangentOut;
        bool isSelected;
        bool smooth;
        string? x;
        string? y;

        public Point Position
        {
            get => position;
            set
            {
                if (SetField(ref position, value))
                {
                    ResetPopup();
                }
            }
        }

        public string X
        {
            get => x ?? position.X.ToString("F4", CultureInfo.InvariantCulture);
            set
            {
                if (SetField(ref x, value))
                {
                    var d = Numbers.ParseDouble(value);
                    if (d.HasValue)
                    {
                        position.X = d.Value;
                        RaisePropertyChanged(nameof(Position));
                    }
                }
            }
        }

        public string Y
        {
            get => y ?? position.Y.ToString("F4", CultureInfo.InvariantCulture);
            set
            {
                if (SetField(ref y, value))
                {
                    var d = Numbers.ParseDouble(value);
                    if (d.HasValue)
                    {
                        position.Y = d.Value;
                        RaisePropertyChanged(nameof(Position));
                    }
                }
            }
        }

        public double TangentIn
        {
            get => tangentIn;
            set
            {
                if (SetField(ref tangentIn, value) && Smooth)
                    TangentOut = -value;
            }
        }

        public double TangentOut
        {
            get => tangentOut;
            set
            {
                if (SetField(ref tangentOut, value) && Smooth)
                    TangentIn = -value;
            }
        }

        public bool IsSelected
        {
            get => isSelected;
            set => SetField(ref isSelected, value);
        }

        public bool Smooth
        {
            get => smooth;
            set
            {
                if (SetField(ref smooth, value) && value)
                    TangentOut = -TangentIn;
            }
        }

        public CurveDialog Dialog { get; }

        public CurveKnot(CurveDialog dialog, Point position, double tangentIn = 0, double tangentOut = 0)
        {
            Dialog = dialog;
            Position = position;
            TangentIn = tangentIn;
            TangentOut = tangentOut;
            Smooth = Numbers.Compare(tangentIn, -tangentOut, 0.0000000001);
        }

        public override string ToString()
        {
            return $"{Position.X:F3} : {Position.Y:F3}";
        }

        internal void ResetPopup()
        {
            x = null;
            y = null;
            RaisePropertyChanged(nameof(X));
            RaisePropertyChanged(nameof(Y));
        }
    }

    public class CurveScaler : NotifyPropertyChanged
    {
        private double pivotX;

        public double PivotX
        {
            get => pivotX;
            set => SetField(ref pivotX, value);
        }

        private double pivotY;

        public double PivotY
        {
            get => pivotY;
            set => SetField(ref pivotY, value);
        }

        private double scaleX = 1;

        public double ScaleX
        {
            get => scaleX;
            set => SetField(ref scaleX, value);
        }

        private double scaleY = 1;

        public double ScaleY
        {
            get => scaleY;
            set => SetField(ref scaleY, value);
        }

        public Vector Scale => new Vector(ScaleX, ScaleY);

        public double Ratio => ScaleY / ScaleX;

        public void Reset()
        {
            ScaleX = 1;
            ScaleY = 1;
            PivotX = 0;
            PivotY = 0;
        }
    }

    public class CurveDialog : Dialog
    {
        public CurveKnot? FocusedKnot
        {
            get => focusedKnot;
            set => SetField(ref focusedKnot, value);
        }

        public bool IsOpenKnotEditor
        {
            get => isOpenKnotEditor;
            set
            {
                if (SetField(ref isOpenKnotEditor, value) && !value)
                {
                    FocusedKnot?.ResetPopup();
                }
            }
        }

        public bool IsScalePopupOpened
        {
            get => isScalePopupOpened;
            set => SetField(ref isScalePopupOpened, value);
        }

        public ObservableCollection<CurveKnot> Knots { get; } = new ObservableCollection<CurveKnot>();

        public EditorCurveData Result =>
            new EditorCurveData(Knots
                .OrderBy(p => p.Position.X)
                .Select(p => new EditorCurveKnot(p.Position, p.TangentIn, p.TangentOut))
                .ToList()
            );

        public ICommand ClearCommand { get; }
        public ICommand AddPresetCommand { get; }
        public ICommand LoadPresetCommand { get; }
        public ICommand StraighteningCommand { get; }
        public ICommand LinearCommand { get; }
        public ICommand CatmullRomCommand { get; }
        public ICommand CancelCommand { get; }

        public ICommand<Point> AddKnotCommand { get; }
        public ICommand<CurveKnot> RemoveKnotCommand { get; }
        public ICommand<CurveKnot> SelectKnotCommand { get; }
        public ICommand<CurveKnot> PopupKnotCommand { get; }
        public ICommand<Point> HidePopupCommand { get; }
        public ICommand<string> UpdateKnotXCommand { get; }
        public ICommand<string> UpdateKnotYCommand { get; }
        public ICommand ScaleCommand { get; }
        public ICommand ApplyScaleCommand { get; }

        public ICommand DeleteCommand { get; }

        public CurveScaler Scaler { get; } = new CurveScaler();
        public string AxisXLabel { get; }
        public string AxisYLabel { get; }

        public Rect Viewport { get; } //For Xaml binding
        private CurveKnot? focusedKnot;
        private bool isOpenKnotEditor;
        private readonly EditorCurve editor;
        private readonly IDialogService dialogService;
        private readonly Action<EditorCurve> saveEditor;
        private bool isScalePopupOpened;

        public CurveDialog(string title, EditorCurve editor, Optional<EditorCurveData> curveData, IDialogService dialogService, Action<EditorCurve> saveEditor)
        {
            Title = title;
            this.editor = editor;
            this.dialogService = dialogService;
            this.saveEditor = saveEditor;
            AxisXLabel = editor.AxisXLabel ?? "X";
            AxisYLabel = editor.AxisYLabel ?? "Y";

            this.Viewport = editor.DefaultViewport.WithRelativeMargin(0.05);

            ClearCommand = Commands.Execute(() => Knots.Clear());
            AddKnotCommand = Commands.Execute<Point>(AddKnot);
            RemoveKnotCommand = Commands.Execute<CurveKnot>(RemoveKnot);
            SelectKnotCommand = Commands.Execute<CurveKnot>((knot) =>
            {
                FocusedKnot = knot;
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    knot.IsSelected = !knot.IsSelected;
                }
                else
                {
                    foreach (var selectedKnot in Knots.Where(x => x.IsSelected && x != knot))
                        selectedKnot.IsSelected = false;
                    knot.IsSelected = true;
                }
            });
            PopupKnotCommand = Commands.Execute<CurveKnot>((knot) =>
            {
                FocusedKnot = knot;
                IsOpenKnotEditor = true;
            });
            HidePopupCommand = Commands.Execute<Point>((position) => IsOpenKnotEditor = false);

            StraighteningCommand = Commands.Execute(Straightening).If(() => FocusedKnot != null);
            UpdateKnotXCommand = Commands.Execute<string>((arg) =>
            {
                var pos = Numbers.ParseDouble(arg);
                if (pos.HasValue)
                    FocusedKnot!.Position = new Point(pos.Value, FocusedKnot.Position.Y);
            });
            UpdateKnotYCommand = Commands.Execute<string>((arg) =>
            {
                var pos = Numbers.ParseDouble(arg);
                if (pos.HasValue)
                    FocusedKnot!.Position = new Point(FocusedKnot.Position.X, pos.Value);
            });
            AddPresetCommand = Commands.Execute(AddPreset);
            LoadPresetCommand = Commands.Execute(LoadPreset);
            DeleteCommand = Commands.Execute(RemoveSelected);
            LinearCommand = Commands.Execute(Linear).If(() => Knots.Count > 1);
            CatmullRomCommand = Commands.Execute(CatmullRom).If(() => Knots.Count > 2);
            CancelCommand = Commands.Execute(Cancel);
            ScaleCommand = Commands.Execute(Scale);
            ApplyScaleCommand = Commands.Execute(ApplyScale);

            if (curveData.HasValue)
                Knots.AddRange(curveData.Value.Knots.Select(p => new CurveKnot(this, p.Position, p.TangentIn, p.TangentOut)));

            if (editor.AutoScaleEditor && Knots.Count >= 2)
            {
                var r = Knots.Select(k => k.Position).GetBounds();
                if (r.Width > 0 && r.Height > 0)
                    Viewport = r.WithRelativeMargin(0.05);
            }
        }

        private void ApplyScale()
        {
            foreach (var knot in Knots)
            {
                knot.Position = knot.Position.ComponentMultiply(Scaler.Scale);
                knot.TangentIn = knot.TangentIn * Scaler.Ratio;
                if (!knot.Smooth)
                    knot.TangentOut = knot.TangentOut * Scaler.Ratio;
            }
            IsScalePopupOpened = false;
        }

        private void Scale()
        {
            Scaler.Reset();
            IsScalePopupOpened = true;
        }

        private void Cancel()
        {
            if (IsOpenKnotEditor)
                IsOpenKnotEditor = false;
            else if (IsScalePopupOpened)
                IsScalePopupOpened = false;
            else
                SetDialogResult(false);
        }

        private void RemoveSelected()
        {
            Knots.RemoveAll(knot => knot.IsSelected);
            FocusedKnot = null;
            IsOpenKnotEditor = false;
        }

        void AddKnot(Point position)
        {
            var newKnot = new CurveKnot(this, position);
            Knots.Add(newKnot);
            FocusedKnot = newKnot;
        }

        void RemoveKnot(CurveKnot knot)
        {
            if (FocusedKnot != null)
            {
                Knots.Remove(FocusedKnot);
            }

            FocusedKnot = null;
            IsOpenKnotEditor = false;
        }

        void AddPreset()
        {
            var presetDialog = new CurvePresetManagerDialog(editor.Presets, editor.DefaultViewport, true);

            if (!(dialogService.ShowDialog(presetDialog)))
                return;

            var updatedPreset = editor.Presets.FirstOrDefault(p => p.Name == presetDialog.SelectedName);

            if (updatedPreset == null) //save new preset
            {
                updatedPreset = new EditorCurvePreset(presetDialog.SelectedName!, Result);
                editor.Presets.Add(updatedPreset);
            }
            else
            {
                updatedPreset.CurveData = Result;
            }

            saveEditor(editor);
        }

        void LoadPreset()
        {
            var presetDialog = new CurvePresetManagerDialog(editor.Presets, editor.DefaultViewport, false);

            if (!(dialogService.ShowDialog(presetDialog)))
                return;

            var preset = presetDialog.SelectedPreset;
            if (preset == null)
                return;

            Knots.Clear();
            Knots.AddRange(preset.CurveData.Knots.Select(p => new CurveKnot(this, p.Position, p.TangentIn, p.TangentOut)));
        }

        void Straightening()
        {
            var knots = Knots.OrderBy(x => x.Position.X).ToList();
            var i = knots.IndexOf(focusedKnot!);
            if (i >= 0 && i < knots.Count - 1)
            {
                var angle = (knots[i + 1].Position.Y - knots[i].Position.Y) / (knots[i + 1].Position.X - knots[i].Position.X);
                knots[i].TangentOut = angle;
                knots[i + 1].TangentIn = -angle;
            }
        }

        void Linear()
        {
            var knots = Knots.OrderBy(x => x.Position.X).ToList();
            for (int i = 0; i < knots.Count - 1; i++)
            {
                var angle = (knots[i + 1].Position.Y - knots[i].Position.Y) / (knots[i + 1].Position.X - knots[i].Position.X);
                knots[i].Smooth = false;
                knots[i + 1].Smooth = false;
                knots[i].TangentOut = angle;
                knots[i + 1].TangentIn = -angle;
            }
        }

        void CatmullRom()
        {
            var knots = Knots.OrderBy(x => x.Position.X).ToList();
            for (int i = 1; i < knots.Count - 1; i++)
            {
                var angle = (knots[i + 1].Position.Y - knots[i - 1].Position.Y) / (knots[i + 1].Position.X - knots[i - 1].Position.X);
                knots[i].TangentOut = angle;
                knots[i].TangentIn = -angle;
                knots[i].Smooth = true;
            }
        }
    }
}