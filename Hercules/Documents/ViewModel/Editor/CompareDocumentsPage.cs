using Hercules.Shell;
using ICSharpCode.AvalonEdit.Document;
using Json;
using JsonDiff;
using System.Windows;
using System.Windows.Input;

namespace Hercules.Documents.Editor
{
    public class CompareDocumentsPage : Page
    {
        IDocument? leftDocument;

        public IDocument? LeftDocument
        {
            get => leftDocument;
            set
            {
                if (leftDocument != value)
                {
                    leftDocument = value;
                    RaisePropertyChanged();
                    ResetDiff();
                }
            }
        }

        IDocument? rightDocument;

        public IDocument? RightDocument
        {
            get => rightDocument;
            set
            {
                if (rightDocument != value)
                {
                    rightDocument = value;
                    RaisePropertyChanged();
                    ResetDiff();
                }
            }
        }

        public ICommand<IDataObject> LeftDropCommand { get; }
        public ICommand<IDataObject> RightDropCommand { get; }

        public TextDocument LeftText { get; }
        public TextDocument RightText { get; }

        IDiffLines? leftDiffLines;

        public IDiffLines? LeftDiffLines
        {
            get => leftDiffLines;
            set => SetField(ref leftDiffLines, value);
        }

        IDiffLines? rightDiffLines;

        public IDiffLines? RightDiffLines
        {
            get => rightDiffLines;
            set => SetField(ref rightDiffLines, value);
        }

        public ICommand SwapCommand { get; }

        public IReadOnlyDatabase Database { get; }

        public CompareDocumentsPage(IReadOnlyDatabase database, IDocument? leftDocument, IDocument? rightDocument)
        {
            this.Database = database;
            this.Title = "Compare Documents";
            this.LeftDocument = leftDocument;
            this.RightDocument = rightDocument;
            this.LeftText = new TextDocument();
            this.RightText = new TextDocument();
            this.LeftDropCommand = Commands.Execute<IDataObject>(LeftDrop).If(CanDrop);
            this.RightDropCommand = Commands.Execute<IDataObject>(RightDrop).If(CanDrop);
            this.SwapCommand = Commands.Execute(Swap);
            ResetDiff();
        }

        private void Swap()
        {
            var acc = LeftDocument;
            LeftDocument = RightDocument;
            RightDocument = acc;
        }

        private void LeftDrop(IDataObject data)
        {
            LeftDocument = HerculesDragData.DragDocument(Database, data);
        }

        private void RightDrop(IDataObject data)
        {
            RightDocument = HerculesDragData.DragDocument(Database, data);
        }

        private bool CanDrop(IDataObject data)
        {
            return HerculesDragData.DragDocument(Database, data) != null;
        }

        private void ResetDiff()
        {
            if (leftDocument != null && rightDocument != null)
            {
                var diff = new JsonDiffEngine().Process(leftDocument.Json, rightDocument.Json);
                var formatter = new JsonCompareFormatter();
                formatter.Process(diff);
                LeftText.Text = formatter.LeftBuilder.ToString();
                RightText.Text = formatter.RightBuilder.ToString();
                LeftDiffLines = new DiffLines(formatter.LeftLines);
                RightDiffLines = new DiffLines(formatter.RightLines);
            }
            else if (leftDocument != null)
            {
                LeftText.Text = leftDocument.Json.ToString(JsonFormat.Multiline);
                LeftDiffLines = null;
            }
            else if (rightDocument != null)
            {
                RightText.Text = rightDocument.Json.ToString(JsonFormat.Multiline);
                RightDiffLines = null;
            }
        }
    }
}
