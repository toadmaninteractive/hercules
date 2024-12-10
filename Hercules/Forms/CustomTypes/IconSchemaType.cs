using Hercules.Documents;
using Hercules.Forms.Schema.Custom;
using Hercules.Shell;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Hercules.Forms.Schema
{
    public class IconSchemaType : CustomSchemaType
    {
        const int MaxThumbnailWidth = 128;
        const int MaxThumbnailHeight = 64;

        public IDialogService DialogService { get; }
        public IDocument EditorDocument { get; }
        public IFile? Atlas { get; }
        public BitmapSource? Image { get; private set; }

        public int ThumbnailWidth { get; private set; }
        public int ThumbnailHeight { get; private set; }

        public EditorIcon Editor { get; }

        public override string Tag => IconCustomTypeSupport.TagValue;

        public int IconWidth => Editor.IconWidth;
        public int IconHeight => Editor.IconHeight;

        public IconSchemaType(SchemaType detailType, EditorIcon editor, IDocument editorDocument, IDialogService dialogService, bool optional = false, string? help = null)
            : base(detailType, optional, help)
        {
            this.DialogService = dialogService;
            this.Editor = editor;
            this.EditorDocument = editorDocument;
            this.Atlas = EditorDocument.CurrentRevision?.Attachments.FirstOrDefault(f => f.Name == Editor.Atlas)?.File;
            SetupThumbnailSize();
            if (Atlas != null)
                LoadImageAsync().Track();
        }

        void SetupThumbnailSize()
        {
            if (IconWidth <= MaxThumbnailWidth && IconHeight <= MaxThumbnailHeight)
            {
                ThumbnailWidth = IconWidth;
                ThumbnailHeight = IconHeight;
            }
            else
            {
                var ratioX = MaxThumbnailWidth / (double)IconWidth;
                var ratioY = MaxThumbnailHeight / (double)IconHeight;
                var ratio = Math.Min(ratioX, ratioY);
                ThumbnailWidth = (int)(IconWidth * ratio);
                ThumbnailHeight = (int)(IconHeight * ratio);
            }
        }

        async Task LoadImageAsync()
        {
            await Atlas!.LoadAsync().ConfigureAwait(true);
            Image = FileUtils.LoadImageFromFile(Atlas!.FileName!);
        }

        public override bool Equals(SchemaType? other)
        {
            if (other is IconSchemaType that)
                return this.Kind == that.Kind && this.Optional == that.Optional && this.Help == that.Help && this.EditorDocument == that.EditorDocument;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), EditorDocument);
        }
    }
}