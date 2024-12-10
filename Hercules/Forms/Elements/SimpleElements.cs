using Hercules.Controls;
using Hercules.Documents;
using Hercules.Forms.Presentation;
using Hercules.Forms.Schema;
using Hercules.Repository;
using ICSharpCode.AvalonEdit.Document;
using Json;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using IDocument = Hercules.Documents.IDocument;

namespace Hercules.Forms.Elements
{
    public abstract class SimpleElement<T> : Element
    {
        [AllowNull]
        protected T value;

        public T Value
        {
            get => this.value;
            set
            {
                var translatedValue = TranslateValue(value);
                if (translatedValue == null || translatedValue.Equals(this.value))
                    return;

                Form.Run(transaction => SetValue(translatedValue, transaction, this), this);
            }
        }

        protected virtual T TranslateValue(T val) => val;

        public void SetValue(T newValue, ITransaction transaction, object? undoRedoGroup = null)
        {
            var oldValue = value;
            UpdateValue(newValue, transaction);

            void Undo(ITransaction t)
            {
                UpdateValue(oldValue, t);
                ValueChanged(t);
            }

            void Redo(ITransaction t)
            {
                UpdateValue(newValue, t);
                ValueChanged(t);
            }

            transaction.AddUndoRedo(Undo, Redo, undoRedoGroup);

            ValueChanged(transaction);
        }

        private void UpdateValue(T newValue, ITransaction transaction)
        {
            this.value = newValue;
            RaisePropertyChanged(nameof(Value));
            ValueChanged(transaction);
        }

        public override ImmutableJson? OriginalJson => originalJson;
        private ImmutableJson? originalJson;

        protected SimpleElement(IContainer parent, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, transaction)
        {
            this.originalJson = originalJson;
        }

        public override void SetOriginalJson(ImmutableJson? newOriginalJson, ITransaction transaction)
        {
            originalJson = newOriginalJson;
        }
    }

    public abstract class SimpleElement<TValueType, TSchemaType> : SimpleElement<TValueType> where TSchemaType : SimpleSchemaType<TValueType>
    {
        public override SchemaType Type => SimpleType;

        public TSchemaType SimpleType { get; }

        protected SimpleElement(IContainer parent, TSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, originalJson, transaction)
        {
            this.SimpleType = type;
            SimpleType.ConvertFromJson(json, out this.value);
        }

        public override ImmutableJson Json => SimpleType.ConvertToJson(Value);

        public override string JsonKey => SimpleType.ConvertToJsonKey(Value);

        protected override void Validate(bool invalidateChildren, ITransaction transaction, out bool isValid, out bool isModified)
        {
            isValid = SimpleType.IsValid(Value);
            isModified = !SimpleType.IsOriginalValue(Value, OriginalJson, IsJsonKey);
        }

        protected override bool IsOverriden(ImmutableJson? baseJson)
        {
            return !SimpleType.IsOriginalValue(Value, baseJson, IsJsonKey);
        }

        protected override TValueType TranslateValue(TValueType val) => SimpleType.TranslateValue(val);

        public override void SetJson(ImmutableJson? json, ITransaction transaction)
        {
            if (json == null && SimpleType.SchemaDefault.HasValue)
            {
                if (value == null || !SimpleType.ValueEquals(value, SimpleType.SchemaDefault.Value))
                    SetValue(SimpleType.SchemaDefault.Value, transaction);
            }
            else
            if (SimpleType.ConvertFromJson(json, out var newVal))
            {
                if (!SimpleType.ValueEquals(value, newVal))
                    SetValue(newVal, transaction);
            }
            else
                transaction.AddWarning(Path, "Invalid value " + json.ToStringSafe());
        }

        public override void SetJsonKey(string jsonKey, ITransaction transaction)
        {
            if (SimpleType.ConvertFromJsonKey(jsonKey, out var newVal))
            {
                if (!SimpleType.ValueEquals(value, newVal))
                    SetValue(newVal, transaction);
            }
            else
                transaction.AddWarning(Path, "Invalid value " + (jsonKey ?? "null"));
        }

        public override int CompareTo(Element? other)
        {
            if (other is SimpleElement<TValueType, TSchemaType> otherSimpleElement)
                return SimpleType.Comparer.Compare(Value, otherSimpleElement.Value);
            else
                return base.CompareTo(other);
        }
    }

    public abstract class NullableElement<TValueType, TSchemaType> : SimpleElement<TValueType?> where TSchemaType : SimpleSchemaType<TValueType> where TValueType : struct
    {
        public override SchemaType Type => SimpleType;

        public TSchemaType SimpleType { get; }

        protected NullableElement(IContainer parent, TSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, originalJson, transaction)
        {
            this.SimpleType = type;
            if (SimpleType.ConvertFromJson(json, out var val))
            {
                this.value = val;
            }
            else
            {
                this.value = SimpleType.Default;
            }
        }

        public override ImmutableJson Json => Value.HasValue ? SimpleType.ConvertToJson(Value.Value) : ImmutableJson.Null;

        public override string JsonKey => Value.HasValue ? SimpleType.ConvertToJsonKey(Value.Value) : string.Empty;

        protected override void Validate(bool invalidateChildren, ITransaction transaction, out bool isValid, out bool isModified)
        {
            isValid = Value.HasValue && SimpleType.IsValid(Value.Value);
            isModified = Value.HasValue ? !SimpleType.IsOriginalValue(Value.Value, OriginalJson, IsJsonKey) : OriginalJson != null;
        }

        protected override bool IsOverriden(ImmutableJson? baseJson)
        {
            return Value.HasValue ? !SimpleType.IsOriginalValue(Value.Value, baseJson, IsJsonKey) : baseJson != null;
        }

        public override void SetJson(ImmutableJson? json, ITransaction transaction)
        {
            if (json == null && SimpleType.SchemaDefault.HasValue)
            {
                if (!value.HasValue || !SimpleType.ValueEquals(value.Value, SimpleType.SchemaDefault.Value))
                    SetValue(SimpleType.SchemaDefault.Value, transaction);
            }
            else if (SimpleType.ConvertFromJson(json, out var newVal))
            {
                if (!value.HasValue || !SimpleType.ValueEquals(value.Value, newVal))
                    SetValue(newVal, transaction);
            }
            else
                transaction.AddWarning(Path, "Invalid value " + json.ToStringSafe());
        }

        public override void SetJsonKey(string jsonKey, ITransaction transaction)
        {
            if (SimpleType.ConvertFromJsonKey(jsonKey, out var newVal))
            {
                if (!value.HasValue || !SimpleType.ValueEquals(value.Value, newVal))
                    SetValue(newVal, transaction);
            }
            else
                transaction.AddWarning(Path, "Invalid value " + (jsonKey ?? "null"));
        }

        public override int CompareTo(Element? other)
        {
            if (other is NullableElement<TValueType, TSchemaType> otherNullableElement)
            {
                var myValue = Value;
                var otherValue = otherNullableElement.Value;
                if (myValue.HasValue && otherValue.HasValue)
                    return SimpleType.Comparer.Compare(myValue.Value, otherValue.Value);
                else if (myValue.HasValue)
                    return 1;
                else if (otherValue.HasValue)
                    return -1;
                else
                    return 0;
            }
            else
            {
                return base.CompareTo(other);
            }
        }
    }

    public abstract class NullableReferenceElement<TValueType, TSchemaType> : SimpleElement<TValueType?, TSchemaType> where TSchemaType : SimpleSchemaType<TValueType?> where TValueType : class
    {
        protected NullableReferenceElement(IContainer parent, TSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, type, json, originalJson, transaction)
        {
        }
    }

    public class KeyElement : NullableReferenceElement<string, KeySchemaType>, IAutoCompleteElement
    {
        public KeyElement(IContainer parent, KeySchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, type, json, originalJson, transaction)
        {
            DropCommand = Commands.Execute<IDataObject>(Drop).If(AllowDrop);
            HasPostUpdate = true;

            Document = SimpleType.SchemafulDatabase.Database.ObserveDocument(Value ?? string.Empty);
        }

        public IObservableDocument Document { get; }

        public ICommand<IDataObject> DropCommand { get; }

        protected override void PostUpdate()
        {
            Document.DocumentId = Value ?? string.Empty;
        }

        bool AllowDrop(IDataObject data)
        {
            var docId = HerculesDragData.DragDocumentId(data);
            return SimpleType.IsValid(docId);
        }

        void Drop(IDataObject data)
        {
            Value = HerculesDragData.DragDocumentId(data);
        }

        public override void Present(PresentationContext context)
        {
            var proxy = context.GetProxy(this);
            if (proxy.Item0 == null)
            {
                var dock = context.FlexibleDock;
                proxy.Item0 = new VirtualRowItem(this, ControlPools.GetPool("KeyElementPreview"), editorPool: ControlPools.GetPool("EnumElementEditor"), popupPool: ControlPools.GetPool("KeyElementPopup"), dock: dock, width: 320);
                proxy.Item0.AddBehavior(new AutoCompleteBehavior(proxy.Item0, this));
            }

            context.AddItem(proxy.Item0);
            if (!context.IsPropertyEditor)
                context.AddItem(proxy.Item1 ??= new VirtualRowItem(this, ControlPools.GetPool("KeyElementHint")));
        }

        object IAutoCompleteElement.Items => SimpleType.Items;

        public void Submit(object? suggestion)
        {
            if (suggestion == null)
            {
                if (SimpleType.Items.Cast<object>().FirstOrDefault(Filter) is IDocument doc)
                {
                    Value = doc.DocumentId;
                }
            }
            else if (suggestion is IDocument doc)
            {
                Value = doc.DocumentId;
            }
            else if (suggestion is string docId)
            {
                Value = docId;
            }
        }

        public bool Filter(object? item)
        {
            if (item != null)
            {
                string filter = Value ?? string.Empty;
                IDocument document = (IDocument)item;
                if (document.DocumentId.SmartFilter(filter))
                    return true;
                if (document.Preview.Caption != null && document.Preview.Caption.SmartFilter(filter))
                    return true;
                return false;
            }
            return false;
        }
    }

    public class BoolElement : SimpleElement<bool, BoolSchemaType>
    {
        public BoolElement(IContainer parent, BoolSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, type, json, originalJson, transaction)
        {

        }

        public override void Present(PresentationContext context)
        {
            var proxy = context.GetProxy(this);
            context.AddItem(proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool(GetType()), isTabStop: true, width: 24, dock: HorizontalDock.Left));
        }
    }

    public class IntElement : NullableElement<int, IntSchemaType>
    {
        public IntElement(IContainer parent, IntSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, type, json, originalJson, transaction)
        {
        }

        public override void Present(PresentationContext context)
        {
            var proxy = context.GetProxy(this);
            if (proxy.Item0 == null)
            {
                var dock = context.FlexibleDock;
                proxy.Item0 = proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool("IntElementPreview"), editorPool: ControlPools.GetPool("IntElementEditor"),
                    popupPool: ControlPools.GetPool("NumberElementPopup"),
                    dock: dock, width: 120);
                proxy.Item0.AddBehavior(new IntEditorBehavior(proxy.Item0, this));
            }
            context.AddItem(proxy.Item0);
            if (SimpleType.IsSlider && !context.IsPropertyEditor && !context.Compact)
                context.AddItem(proxy.Item1 ??= new VirtualRowItem(this, ControlPools.GetPool("IntElementSlider")));
        }

        protected override void OnValueUpdate(ITransaction transaction)
        {
            base.OnValueUpdate(transaction);
            RaisePropertyChanged(nameof(DisplayValue));
        }

        public string DisplayValue => Value?.ToString(SimpleType.StringFormat, SimpleType.NumberFormat) ?? "";
    }

    public class FloatElement : NullableElement<double, FloatSchemaType>
    {
        public FloatElement(IContainer parent, FloatSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, type, json, originalJson, transaction)
        {
        }

        public override void Present(PresentationContext context)
        {
            var proxy = context.GetProxy(this);
            if (proxy.Item0 == null)
            {
                var dock = context.FlexibleDock;
                proxy.Item0 = proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool("FloatElementPreview"), editorPool: ControlPools.GetPool("FloatElementEditor"),
                    popupPool: ControlPools.GetPool("NumberElementPopup"),
                    dock: dock, width: 120);
                proxy.Item0.AddBehavior(new FloatEditorBehavior(proxy.Item0, this));
            }
            context.AddItem(proxy.Item0);
            if (SimpleType.IsSlider && !context.IsPropertyEditor && !context.Compact)
                context.AddItem(proxy.Item1 ??= new VirtualRowItem(this, ControlPools.GetPool("FloatElementSlider")));
        }

        protected override void OnValueUpdate(ITransaction transaction)
        {
            base.OnValueUpdate(transaction);
            RaisePropertyChanged(nameof(DisplayValue));
        }

        public string DisplayValue => Value?.ToString(SimpleType.StringFormat, SimpleType.NumberFormat) ?? "";
    }

    public class StringElement : NullableReferenceElement<string, StringSchemaType>
    {
        public StringElement(IContainer parent, StringSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, type, json, originalJson, transaction)
        {
            ClearCommand = Commands.Execute(DoClear);
            if (type.ReferenceSourceId != null)
                Form.GetService<ElementReferenceService>().RegisterSource(type.ReferenceSourceId, this, SimpleType.UniqueReference);
        }

        public ICommand ClearCommand { get; }

        void DoClear()
        {
            Value = SimpleType.Default;
        }

        public override void Present(PresentationContext context)
        {
            var proxy = context.GetProxy(this);
            context.AddItem(proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool("StringElementPreview"), editorPool: ControlPools.GetPool("StringElementEditor"), dock: context.FillDock));
        }
    }

    public class PathElement : NullableReferenceElement<string, PathSchemaType>
    {
        public PathElement(IContainer parent, PathSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, type, json, originalJson, transaction)
        {
            ClearCommand = Commands.Execute(() => Value = SimpleType.Default);
            OpenFileCommand = Commands.Execute(DoOpenFile);
            SetFlag(ElementFlags.HasPreview, HasPreview());
        }

        public ICommand ClearCommand { get; }
        public ICommand OpenFileCommand { get; }

        private IReadOnlyObservableValue<BitmapSource>? image;
        public IReadOnlyObservableValue<BitmapSource>? Image
        {
            get
            {
                if (SimpleType.ProjectSettings?.Repository == null)
                    return null;

                if (image == null)
                {
                    var filename = this.ObserveProperty(nameof(Value), e => e.SimpleType.GetRelativeFileName(e.Value));
                    image = SimpleType.ProjectSettings.Repository!.ObserveImage(filename, FileUtils.NoImage);
                }
                return image;
            }
        }

        void DoOpenFile()
        {
            if (SimpleType.ProjectSettings == null)
                return;

            if (SimpleType.ProjectSettings.Repository == null)
                if (!SimpleType.ProjectSettings.ShowDialog())
                    return;

            if (SimpleType.ProjectSettings.Repository == null)
                return;

            var defaultExtension = SimpleType.Extension;
            if (SimpleType.UnrealClassPath || SimpleType.UnrealAssetPath)
                defaultExtension = "uasset";

            string? fileName = null;
            if (!string.IsNullOrWhiteSpace(value))
            {
                fileName = value;
                if (SimpleType.UnrealClassPath || SimpleType.UnrealAssetPath)
                    fileName = value.TryReplacePrefix("/Game", "Content", StringComparison.OrdinalIgnoreCase);

                if (SimpleType.UnrealClassPath || SimpleType.UnrealAssetPath)
                {
                    fileName = System.IO.Path.ChangeExtension(fileName, ".uasset");
                }
                else if (!SimpleType.IncludeExtension && string.IsNullOrEmpty(System.IO.Path.GetExtension(fileName)) && !string.IsNullOrEmpty(defaultExtension))
                {
                    fileName = $"{fileName}.{defaultExtension}";
                }
            }

            var dialogParams = new BrowseRepositoryDialogParams(RootPath: SimpleType.Root, DefaultPath: SimpleType.DefaultPath,
                InitialFileName: fileName, DefaultExtension: defaultExtension, Preview: SimpleType.Preview);
            if (SimpleType.ProjectSettings.Repository.Browse($"Pick file: {Path}", dialogParams, out var relativePath))
            {
                if (!SimpleType.IncludeExtension)
                {
                    relativePath = System.IO.Path.ChangeExtension(relativePath, null);
                }

                if (SimpleType.UnrealClassPath || SimpleType.UnrealAssetPath)
                {
                    relativePath = relativePath.TryReplacePrefix("Content", "/Game", StringComparison.OrdinalIgnoreCase);
                    var suffix = SimpleType.UnrealClassPath ? "_C" : "";
                    relativePath = System.IO.Path.ChangeExtension(relativePath, System.IO.Path.GetFileNameWithoutExtension(relativePath) + suffix);
                }
                Value = relativePath;
            }
        }

        public double PreviewWidth => SimpleType.PreviewWidth ?? 200;
        public double PreviewHeight => SimpleType.PreviewHeight ?? 100;

        VirtualRowItem? editItem;
        VirtualRowItem? pathButtonItem;
        VirtualRowItem? previewItem;

        public override void Present(PresentationContext context)
        {
            var proxy = context.GetProxy(this);
            var left = context.Left;
            context.AddItem(editItem ??= new VirtualRowItem(this, ControlPools.GetPool("StringElementPreview"), editorPool: ControlPools.GetPool("StringElementEditor"), dock: context.FillDock));
            context.AddItem(pathButtonItem ??= new VirtualRowItem(this, ControlPools.GetPool("PathElementButton"), dock: context.RightDock, width: 24));
            if (GetFlag(ElementFlags.HasPreview))
            {
                if (!context.IsPropertyEditor)
                    context.Indent(left);
                context.AddRow(proxy);
                context.AddItem(previewItem ??= new VirtualRowItem(this, ControlPools.GetPool("PathElementPreview")), height: PreviewHeight + 2);
                if (!context.IsPropertyEditor)
                    context.Outdent();
            }
        }

        private bool HasPreview() => SimpleType.Preview && SimpleType.ProjectSettings?.Repository != null && !string.IsNullOrWhiteSpace(value);

        protected override void OnValueUpdate(ITransaction transaction)
        {
            base.OnValueUpdate(transaction);
            if (SetFlag(ElementFlags.HasPreview, HasPreview()))
            {
                transaction.RefreshPresentation();
            }
            RaisePropertyChanged(nameof(Image));
        }
    }

    public class TextElement : NullableReferenceElement<string, TextSchemaType>
    {
        public TextElement(IContainer parent, TextSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, type, json, originalJson, transaction)
        {
            ClearCommand = Commands.Execute(DoClear);
            editorHeight = 100;
        }

        public ICommand ClearCommand { get; }

        void DoClear()
        {
            Value = SimpleType.Default;
        }

        double editorHeight;

        public double EditorHeight
        {
            get => editorHeight;
            set
            {
                if (SetField(ref editorHeight, value))
                {
                    Form.RefreshPresentation();
                }
            }
        }

        public override void Present(PresentationContext context)
        {
            var proxy = context.GetProxy(this);
            context.AddItem(proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool(GetType()), isTabStop: true, dock: context.FillDock), height: EditorHeight);
        }
    }

    public class AvalonTextElement : NullableReferenceElement<string, TextSchemaType>
    {
        public AvalonTextElement(IContainer parent, TextSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, type, json, originalJson, transaction)
        {
            Document = new TextDocument(value);
            Document.TextChanged += Document_TextChanged;
            editorHeight = 100;
            HasPostUpdate = true;
            // Document.PropertyChanged += Document_PropertyChanged;
        }

        double editorHeight;

        public double EditorHeight
        {
            get => editorHeight;
            set
            {
                if (SetField(ref editorHeight, value))
                {
                    Form.RefreshPresentation();
                }
            }
        }

        /*private void Document_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TextDocument.LineCount))
            {
                Form.RebuildCanvas();
                RaisePropertyChanged(nameof(Height));
            }
        }*/

        void Document_TextChanged(object? sender, EventArgs e)
        {
            Value = Document.Text;
        }

        protected override void PostUpdate()
        {
            if (Document.Text != Value)
                Document.Text = Value;
        }

        public TextDocument Document { get; }

        // public override double Height => Math.Max(20, Math.Min(4 + Document.LineCount * 14, 300));
        public override void Present(PresentationContext context)
        {
            var proxy = context.GetProxy(this);
            context.AddItem(proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool(GetType()), isTabStop: true, dock: context.FillDock), height: EditorHeight);
        }
    }

    public class SelectStringElement : NullableReferenceElement<string, SelectStringSchemaType>, IAutoCompleteElement
    {
        public SelectStringElement(IContainer parent, SelectStringSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, type, json, originalJson, transaction)
        {
        }

        public override void Present(PresentationContext context)
        {
            var proxy = context.GetProxy(this);
            if (proxy.Item0 == null)
            {
                var dock = context.FlexibleDock;
                proxy.Item0 = new VirtualRowItem(this, ControlPools.GetPool("EnumElementPreview"), editorPool: ControlPools.GetPool("EnumElementEditor"), popupPool: ControlPools.GetPool("EnumElementPopup"), dock: dock, width: 320);
                proxy.Item0.AddBehavior(new AutoCompleteBehavior(proxy.Item0, this));
            }
            context.AddItem(proxy.Item0);
        }

        object IAutoCompleteElement.Items => SimpleType.Items;

        public void Submit(object? suggestion)
        {
            if (suggestion == null)
            {
                if (SimpleType.Items.FirstOrDefault(Filter) is string value)
                {
                    Value = value;
                }
            }
            else
            {
                Value = (string)suggestion;
            }
        }

        public bool Filter(object? item)
        {
            if (item != null)
            {
                string filter = Value ?? string.Empty;
                string val = (string)item;
                if (val.SmartFilter(filter))
                    return true;
                return false;
            }
            return false;
        }
    }

    public class EnumElement : NullableReferenceElement<string, EnumSchemaType>, IAutoCompleteElement
    {
        public EnumElement(IContainer parent, EnumSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, type, json, originalJson, transaction)
        {
        }

        public override void Present(PresentationContext context)
        {
            var proxy = context.GetProxy(this);
            if (proxy.Item0 == null)
            {
                var dock = context.FlexibleDock;
                proxy.Item0 = new VirtualRowItem(this, ControlPools.GetPool("EnumElementPreview"), editorPool: ControlPools.GetPool("EnumElementEditor"), popupPool: ControlPools.GetPool("EnumElementPopup"), dock: dock, width: 320);
                proxy.Item0.AddBehavior(new AutoCompleteBehavior(proxy.Item0, this));
            }
            context.AddItem(proxy.Item0);
        }

        object IAutoCompleteElement.Items => SimpleType.Enum.Values;

        public void Submit(object? suggestion)
        {
            if (suggestion == null)
            {
                if (SimpleType.Enum.Values.FirstOrDefault(Filter) is string value)
                {
                    Value = value;
                }
            }
            else
            {
                Value = (string)suggestion;
            }
        }

        public bool Filter(object? item)
        {
            if (item != null)
            {
                string filter = Value ?? string.Empty;
                string val = (string)item;
                if (val.SmartFilter(filter))
                    return true;
                return false;
            }
            return false;
        }
    }

    public class BinaryElement : NullableReferenceElement<byte[], BinarySchemaType>
    {
        public BinaryElement(IContainer parent, BinarySchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, type, json, originalJson, transaction)
        {
            LoadCommand = Commands.Execute(DoLoad);
            SaveCommand = Commands.Execute(DoSave);
            HasPostUpdate = true;
        }

        protected override void PostUpdate()
        {
            RaisePropertyChanged(nameof(Caption));
        }

        public int Size => Value?.Length ?? 0;

        public string Caption
        {
            get
            {
                if (Value == null)
                    return "[Binary: no value]";
                else
                    return $"[Binary: {Value.Length} bytes]";
            }
        }

        public ICommand LoadCommand { get; }
        public ICommand SaveCommand { get; }

        void DoLoad()
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Title = "Load Binary"
            };

            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                var newData = File.ReadAllBytes(dlg.FileName);
                Value = newData;
            }
        }

        void DoSave()
        {
            var dlg = new SaveFileDialog
            {
                Title = "Save Binary"
            };

            bool? result = dlg.ShowDialog();

            if (result == true)
                File.WriteAllBytes(dlg.FileName, Value ?? SimpleType.Default ?? Array.Empty<byte>());
        }

        public override void Present(PresentationContext context)
        {
            var proxy = context.GetProxy(this);
            context.AddItem(proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool(GetType())), height: 22);
        }
    }

    public class DateTimeElement : NullableElement<DateTime, DateTimeSchemaType>
    {
        public DateTimeElement(IContainer parent, DateTimeSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, type, json, originalJson, transaction)
        {
        }

        public override void Present(PresentationContext context)
        {
            var proxy = context.GetProxy(this);
            var dock = context.FlexibleDock;
            context.AddItem(proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool("DateTimeElementPreview"), editorPool: ControlPools.GetPool("DateTimeElementEditor"), dock: dock, width: 200));
            if (!context.IsPropertyEditor)
                context.AddItem(proxy.Item1 ??= new VirtualRowItem(this, ControlPools.GetPool("DateTimeElementTimeZone")));
        }
    }

    public class MultiSelectElement : NullableReferenceElement<IReadOnlyList<string>, MultiSelectSchemaType>
    {
        public MultiSelectElement(IContainer parent, MultiSelectSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, type, json, originalJson, transaction)
        {
        }

        public MultiSelectSchemaType MultiSelectType => (MultiSelectSchemaType)Type;

        public IReadOnlyList<string> Items => MultiSelectType.Items;

        public override void Present(PresentationContext context)
        {
            var proxy = context.GetProxy(this);
            var dock = context.FlexibleDock;
            context.AddItem(proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool(GetType()), isTabStop: true, dock: dock, width: 320));
        }
    }
}