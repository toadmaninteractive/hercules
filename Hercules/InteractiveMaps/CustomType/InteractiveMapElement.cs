using Hercules.Documents.Editor;
using Hercules.Forms.Elements;
using Hercules.Forms.Presentation;
using Hercules.Forms.Schema.Custom;
using Hercules.Shell;
using Json;
using System;
using System.Linq;
using System.Windows.Media.Imaging;

namespace Hercules.InteractiveMaps
{
    public class InteractiveMapElement : EditableCustomProxy<InteractiveMapSchemaType>
    {
        public DocumentEditorPage? DocumentEditorPage { get; }
        public EditorInteractiveMap Editor => CustomType.Editor;
        private InteractiveMapTab? tab;

        string? errorMessage;
        private readonly PropertyEditorTool propertyEditorTool;

        public string? ErrorMessage
        {
            get => errorMessage;
            set => SetField(ref errorMessage, value);
        }

        public bool HasError => string.IsNullOrEmpty(errorMessage);

        public InteractiveMapElement(IContainer parent, InteractiveMapSchemaType customType, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction, DocumentEditorPage? documentEditorPage, PropertyEditorTool propertyEditorTool)
            : base(parent, customType, json, originalJson, transaction)
        {
            DocumentEditorPage = documentEditorPage;
            this.propertyEditorTool = propertyEditorTool;
        }

        protected override void Edit()
        {
            SetupInteractiveMap();
        }

        public ListElement GetBlocks()
        {
            return GetByPath(new JsonPath("blocks")) as ListElement;
        }

        public StringElement GetImageFile()
        {
            return GetByPath(new JsonPath("image_file")) as StringElement;
        }

        private void SetupInteractiveMap()
        {
            ErrorMessage = null;

            var imageName = GetImageFile().Value;

            if (string.IsNullOrEmpty(imageName))
            {
                ErrorMessage = "Image not set: attach at least one image to this document, expand map editor and put file name into image_file field";
                // Logger.LogError("Image not set: attach at least one image to this document, expand map editor and put file name into image_file field");
                return;
            }

            var attachment = DocumentEditorPage.Attachments.Items.FirstOrDefault(d => d.Name == imageName);

            if (attachment == null)
            {
                ErrorMessage = $"Cannot find attachment {imageName}: attach at least one image to this document, expand map editor and put file name into image_file field";
                // Logger.LogError($"Cannot find attachment {imageName}: attach at least one image to this document, expand map editor and put file name into image_file field");
                return;
            }

            BitmapSource? image = null;

            try
            {
                image = FileUtils.LoadImageFromFile(attachment.Attachment.File.FileName);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Cannot load image {imageName}. Exception: {ex.Message}";
                // Logger.LogException($"Cannot load image {imageName}", ex);
                return;
            }

            if (tab == null)
            {
                tab = new InteractiveMapTab(DocumentEditorPage, this, propertyEditorTool);
                DocumentEditorPage.Tabs.Insert(0, tab);
            }

            tab.ApplyDataFromElement();
            DocumentEditorPage.ActiveTab = tab;

            // Active form editor
            // Form.Editor.Handler.Activate();
        }

        public override void Present(PresentationContext context)
        {
            var proxy = context.GetProxy(this);
            var left = context.Left;
            context.AddItem(proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool(GetType()), width: 100));
            if (IsExpanded)
            {
                context.Indent(left);
                context.AddRow(proxy);
                Element.Present(context);
                context.Outdent();
            }
        }
    }
}