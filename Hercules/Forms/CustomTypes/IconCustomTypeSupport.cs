using Hercules.Documents;
using Hercules.Forms.Elements;
using Hercules.Forms.Schema;
using Hercules.Forms.Schema.Custom;
using Hercules.Shell;
using Json;
using System.Collections.Generic;
using System.IO;

namespace Hercules.Forms
{
    public class IconCustomTypeSupport : CustomTypeSupport
    {
        public const string TagValue = "icon";

        private readonly IDialogService dialogService;

        public IconCustomTypeSupport(FormSchema editorMetaSchema, IDialogService dialogService)
            : base(editorMetaSchema)
        {
            this.dialogService = dialogService;
        }

        public override Element CreateElement(IContainer parent, CustomSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction, ElementFactoryContext context)
        {
            var iconType = (IconSchemaType)type;
            return new IconElement(parent, iconType, json, originalJson, transaction);
        }

        public override CustomSchemaType CreateSchemaType(IDocument editorDocument, SchemaType contentType, bool optional = false, string? help = null)
        {
            var editor = EditorIconJsonSerializer.Instance.Deserialize(editorDocument.Json);
            return new IconSchemaType(contentType, editor, editorDocument, dialogService, optional, help);
        }

        public override string Tag => TagValue;

        public override DocumentDraft CreateEditorDraft(string documentId, TempStorage tempStorage)
        {
            var iconEditor = new EditorIcon();
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Open Icon Atlas"
            };
            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                iconEditor.Atlas = Path.GetFileName(dlg.FileName);
                var attachment = new AttachmentDraft(documentId, iconEditor.Atlas, tempStorage.CreateFile(dlg.FileName));
                return new DocumentDraft(EditorIconJsonSerializer.Instance.Serialize(iconEditor).AsObject, new List<Attachment> { attachment });
            }
            else
            {
                iconEditor.Atlas = string.Empty;
                return new DocumentDraft(EditorIconJsonSerializer.Instance.Serialize(iconEditor).AsObject);
            }
        }
    }
}