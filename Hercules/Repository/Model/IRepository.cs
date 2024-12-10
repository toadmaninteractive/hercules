using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Hercules.Repository
{
    public record BrowseRepositoryDialogParams(string? RootPath = null, string? DefaultPath = null, string? InitialFileName = null, string? DefaultExtension = null, bool Preview = false);

    public interface IRepository
    {
        PathStyle PathStyle { get; }

        bool Browse(string title, BrowseRepositoryDialogParams dialogParams, [MaybeNullWhen(false)] out string filePath);
        IReadOnlyObservableValue<BitmapSource> ObserveImage(IReadOnlyObservableValue<string?> filename, BitmapSource defaultBitmap);
        Task RefreshFolderAsync(RepositoryFolder folder, CancellationToken ct);
        Task<BitmapSource> DownloadImageAsync(string filename, CancellationToken ct);
    }
}
