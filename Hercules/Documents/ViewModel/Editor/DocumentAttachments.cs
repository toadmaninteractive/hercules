using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Hercules.Documents.Editor
{
    public class DocumentAttachment : NotifyPropertyChanged
    {
        public AttachmentRevision? OriginalAttachment { get; private set; }

        public string Name { get; }

        Attachment attachment;

        public Attachment Attachment
        {
            get => attachment;
            set
            {
                if (SetField(ref attachment, value))
                {
                    RaisePropertyChanged(nameof(IsUpdated));
                    attachments.UpdateModified();
                }
            }
        }

        public ICommand OpenCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RevertCommand { get; }
        public ICommand DragCommand { get; }
        public ICommand ReplaceCommand { get; }

        public bool IsNew => OriginalAttachment == null;
        public bool IsUpdated => Attachment != OriginalAttachment && OriginalAttachment != null;

        bool isDeleted;

        public bool IsDeleted
        {
            get => isDeleted;
            set
            {
                if (SetField(ref isDeleted, value))
                    attachments.UpdateModified();
            }
        }

        readonly DocumentAttachments attachments;

        public DocumentAttachment(DocumentAttachments attachments, Attachment attachment, AttachmentRevision? originalRevision)
        {
            this.attachments = attachments;
            this.Name = attachment.Name;
            this.OriginalAttachment = originalRevision;
            this.attachment = attachment;
            OpenCommand = Commands.Execute(Open).If(() => Attachment.File.IsLoaded);
            DeleteCommand = Commands.Execute(Delete);
            RevertCommand = Commands.Execute(Revert);
            DragCommand = Commands.Execute<DependencyObject>(Drag).If(() => Attachment.File.IsLoaded);
            ReplaceCommand = Commands.Execute(Replace);
        }

        public void Replace(TempFile newFile)
        {
            Attachment = new AttachmentDraft(Attachment.DocumentId, Attachment.Name, newFile);
        }

        public void Rebase(Attachment baseAttachment)
        {
            if (OriginalAttachment != null && Attachment == OriginalAttachment)
            {
                OriginalAttachment = null;
                Attachment = baseAttachment;
            }
        }

        void Replace()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Add Attachment"
            };
            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                Replace(attachments.TempStorage.CreateFile(dlg.FileName));
            }
        }

        void Open()
        {
            Process.Start(new ProcessStartInfo { FileName = Attachment.File.FileName, UseShellExecute = true });
        }

        void Drag(DependencyObject dragSource)
        {
            DragDrop.DoDragDrop(dragSource, new DataObject(DataFormats.FileDrop, new[] { Attachment.File.FileName }), DragDropEffects.Copy);
        }

        void Delete()
        {
            if (IsNew)
                attachments.Items.Remove(this);
            IsDeleted = true;
        }

        void Revert()
        {
            if (IsDeleted)
                IsDeleted = false;
            else if (IsUpdated)
                Attachment = OriginalAttachment;
        }
    }

    public class DocumentAttachments : NotifyPropertyChanged
    {
        public string DocumentId { get; }
        public TempStorage TempStorage { get; }
        public ObservableCollection<DocumentAttachment> Items { get; } = new ObservableCollection<DocumentAttachment>();

        readonly ObservableValue<bool> isModified = new ObservableValue<bool>(false);

        public IReadOnlyObservableValue<bool> IsModified => isModified;

        public ICommand AddCommand { get; }
        public ICommand<IDataObject> DropCommand { get; }

        public DocumentAttachments(string documentId, TempStorage tempStorage)
        {
            DocumentId = documentId;
            TempStorage = tempStorage;
            AddCommand = Commands.Execute(Add);
            DropCommand = Commands.Execute<IDataObject>(Drop).If(CanDrop);
        }

        void Drop(IDataObject data)
        {
            if (data.GetData(DataFormats.FileDrop) is IEnumerable<string> files)
            {
                foreach (var file in files)
                {
                    AddFile(file);
                }
            }
        }

        bool CanDrop(IDataObject data)
        {
            return data.GetDataPresent(DataFormats.FileDrop);
        }

        void Add()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Add Attachment"
            };
            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                AddFile(dlg.FileName);
            }
        }

        void AddFile(string fileName)
        {
            var name = Path.GetFileName(fileName);
            var oldItem = Items.FirstOrDefault(i => i.Name == name);
            var tempFile = TempStorage.CreateFile(fileName);
            if (oldItem != null)
                oldItem.Replace(tempFile);
            else
                Items.Add(new DocumentAttachment(this, new AttachmentDraft(DocumentId, name, tempFile), null));
            UpdateModified();
        }

        public void Setup(IEnumerable<Attachment> attachments)
        {
            Items.Clear();
            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    Items.Add(new DocumentAttachment(this, attachment, attachment as AttachmentRevision));
                }
            }
            UpdateModified();
        }

        public void Rebase(IEnumerable<Attachment> baseAttachments)
        {
            foreach (var attachment in baseAttachments)
            {
                var curAttachment = Items.FirstOrDefault(a => a.Name == attachment.Name);
                curAttachment?.Rebase(attachment);
            }
        }

        public void UpdateModified()
        {
            isModified.Value = Items.Any(i => i.IsNew || i.IsDeleted || i.IsUpdated);
        }

        public IReadOnlyList<Attachment> GetDraft()
        {
            return Items.Where(a => !a.IsDeleted).Select(a => a.Attachment).ToList();
        }
    }
}