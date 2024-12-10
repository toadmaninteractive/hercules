using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Hercules.Shell;

namespace Hercules.Repository
{
    public class BrowseRepositoryDialog : Dialog
    {
        public BrowseRepositoryDialog(string title, IRepository repository, BrowseRepositoryDialogParams dialogParams)
        {
            this.Title = title;
            this.repository = repository;
            this.selectedPath = dialogParams.InitialFileName;
            ShowPreview = dialogParams.Preview;
            rootPath = repository.PathStyle.NormalizeDelimiters(dialogParams.RootPath);
            var initialFileName = repository.PathStyle.NormalizeDelimiters(dialogParams.InitialFileName);
            var defaultPath = repository.PathStyle.NormalizeDelimiters(dialogParams.DefaultPath);
            RootFolder = new RepositoryFolder(repository, rootPath == "" ? "." : rootPath, rootPath);
            RootFolder.LoadAsync().Track();
            if (initialFileName != "")
            {
                OpenPathAsync(repository.PathStyle.GetDirectory(initialFileName), repository.PathStyle.GetFileName(initialFileName), startOpenCts.Token).Track();
            }
            else if (defaultPath != "")
            {
                OpenPathAsync(defaultPath, null, startOpenCts.Token).Track();
            }
            SelectedFolder = RootFolder;
            Root = new[] { RootFolder };
            ForwardCommand = Commands.Execute(NavigateForward).If(CanNavigateForward);
            BackCommand = Commands.Execute(NavigateBack).If(CanNavigateBack);
        }

        private readonly List<RepositoryFolder> history = new();
        private readonly IRepository repository;
        private readonly string rootPath;
        private int historyIndex = -1;
        private readonly CancellationTokenSource startOpenCts = new();

        public bool ShowPreview { get; }
        public ICommand ForwardCommand { get; }
        public ICommand BackCommand { get; }

        public RepositoryFolder RootFolder { get; }
        public IReadOnlyList<RepositoryFolder> Root { get; }

        private RepositoryFolder? selectedFolder;
        public RepositoryFolder? SelectedFolder
        {
            get => selectedFolder;
            set
            {
                if (SetField(ref selectedFolder, value))
                {
                    if (selectedFolder != null)
                    {
                        history.RemoveRange(historyIndex + 1, history.Count - (historyIndex + 1));
                        history.Add(selectedFolder);
                        historyIndex++;
                        selectedFolder.LoadAsync().Track();
                    }
                }
            }
        }

        private RepositoryFile? selectedFile;
        public RepositoryFile? SelectedFile
        {
            get => selectedFile;
            set
            {
                if (SetField(ref selectedFile, value))
                {
                    if (selectedFile != null)
                        SelectedPath = selectedFile.Path.RemovePrefix(rootPath).RemovePrefix("/");
                    if (ShowPreview)
                        SelectedFile?.LoadAsync().Track();
                }
            }
        }

        private string? selectedPath;

        public string? SelectedPath
        {
            get => selectedPath;
            set => SetField(ref selectedPath, value);
        }

        protected override bool IsOkEnabled()
        {
            return !string.IsNullOrWhiteSpace(selectedPath);
        }

        protected override void OnClose(bool result)
        {
            startOpenCts.Cancel();
        }

        private bool CanNavigateForward() => historyIndex < history.Count - 1;
        private bool CanNavigateBack() => historyIndex >= 1;

        private void NavigateForward()
        {
            historyIndex++;
            selectedFolder = history[historyIndex];
            RaisePropertyChanged(nameof(SelectedFolder));
        }

        private void NavigateBack()
        {
            historyIndex--;
            selectedFolder = history[historyIndex];
            RaisePropertyChanged(nameof(SelectedFolder));
        }

        public async Task OpenPathAsync(string path, string? filename, CancellationToken ct)
        {
            var pathParts = repository.PathStyle.PathParts(path);
            var currentFolder = RootFolder;
            foreach (var pathPart in pathParts)
            {
                await currentFolder.LoadAsync();
                currentFolder.IsExpanded = true;
                var next = currentFolder.Folders.FirstOrDefault(f => f.Name.Equals(pathPart, StringComparison.OrdinalIgnoreCase));
                if (next == null)
                {
                    SelectedFolder = next;
                    return;
                }
                currentFolder = next;
            }

            SelectedFolder = currentFolder;
            await currentFolder.LoadAsync();
            if (filename != null)
            {
                SelectedFile = SelectedFolder.Files.FirstOrDefault(f => f.Name == filename);
            }
        }
    }
}
