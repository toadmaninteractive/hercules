using Hercules.Documents;
using Hercules.Forms.Schema;
using Hercules.Shell;
using Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Hercules.Cards
{
    public class Card : NotifyPropertyChanged
    {
        public Card(IDocument document, IReadOnlyObservableValue<BitmapSource>? image, ICommand<IDocument> EditDocumentCommand)
        {
            Document = document;
            Image = image;
            OpenCommand = EditDocumentCommand.For(Document);
        }

        public IDocument Document { get; }

        public IReadOnlyObservableValue<BitmapSource>? Image { get; }

        public ICommand OpenCommand { get; }
    }

    public class CardViewPage : Page, IWorkspaceContextMenuProvider
    {
        public CardViewPage(Project project, DocumentsModule documentsModule, Category category, UiOptionManager optionManager)
        {
            ContextMenu = optionManager.GetContextMenu<IDocument>();
            Project = project;
            DocumentsModule = documentsModule;
            Category = category;
            Title = $"Tiles: {Category.Name}";
            SchemaRecord = Project.SchemafulDatabase.Schema?.Variant?.GetChild(category.Name) ?? SchemaRecord.Default;
            if (SchemaRecord.ImagePath != null)
            {
                imagePath = SchemaRecord.ImagePath;
                imagePathType = SchemaRecord.GetByPath(SchemaRecord.ImagePath) as PathSchemaType;
            }
            Cards.AddRange(Category.Documents.Select(d => new Card(d, ObserveImage(d), documentsModule.EditDocumentCommand.Single)));
            smoothTileSizeSubscription = this.OnPropertyChanged(nameof(TileSize), host => host.TileSize).Throttle(TimeSpan.FromMilliseconds(50)).Subscribe(s => SmoothTileSize = s);
        }

        private readonly IDisposable smoothTileSizeSubscription;

        public Project Project { get; }
        public DocumentsModule DocumentsModule { get; }
        public SchemaRecord SchemaRecord { get; }
        public Category Category { get; }
        public ObservableCollection<Card> Cards { get; } = new();
        public IList? SelectedItems { get; set; }
        public WorkspaceContextMenu? ContextMenu { get; }

        private readonly PathSchemaType? imagePathType;
        private readonly JsonPath? imagePath;

        private double tileSize = 250;

        public double TileSize
        {
            get => tileSize;
            set => SetField(ref tileSize, value);
        }

        private double smoothTileSize = 250;
        public double SmoothTileSize
        {
            get => smoothTileSize;
            set => SetField(ref smoothTileSize, value);
        }

        public override object? GetCommandParameter(Type type)
        {
            if (type == typeof(IDocument))
                return GetActiveDocument();
            if (type == typeof(IReadOnlyCollection<IDocument>))
                return GetSelectedDocuments();
            return base.GetCommandParameter(type);
        }

        public IDocument? GetActiveDocument()
        {
            if (SelectedItems == null)
                return null;
            var docs = SelectedItems.OfType<Card>();
            var count = docs.Count();
            if (count == 1)
                return docs.First().Document;
            else
                return null;
        }

        public IReadOnlyCollection<IDocument>? GetSelectedDocuments()
        {
            return SelectedItems?.OfType<Card>().Select(d => d.Document).ToList();
        }

        private IReadOnlyObservableValue<BitmapSource> ObserveImage(IDocument document)
        {
            var repo = Project.Settings.Repository;
            if (repo == null)
                return new ObservableValue<BitmapSource>(FileUtils.NoImage);
            var filename = GetImageFileName(document);
            return repo.ObserveImage(new ObservableValue<string?>(filename), FileUtils.NoImage);
        }

        private string? GetImageFileName(IDocument document)
        {
            if (imagePath == null || imagePathType == null)
                return null;
            if (!document.Json.TryFetch(imagePath, out var pathJson) || !pathJson.IsString)
                return null;
            return imagePathType.GetRelativeFileName(pathJson.AsString);
        }
    }
}
