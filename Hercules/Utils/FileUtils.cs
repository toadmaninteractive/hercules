using Json;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Media.Imaging;
using Unreal;

namespace Hercules
{
    public static class FileUtils
    {
        public static void Open(IFile file)
        {
            if (file.IsLoaded)
                Process.Start(new ProcessStartInfo { FileName = file.FileName, UseShellExecute = true });
        }

        public static ImmutableJson LoadJsonFromFile(string path)
        {
            using var stream = File.OpenRead(path);
            var size = checked((int)stream.Length);
            using var memoryOwner = MemoryPool<byte>.Shared.Rent(size);
            var span = memoryOwner.Memory.Span.Slice(0, size);
            stream.Read(span);
            var jsonReader = new Utf8JsonReader(span);
            return Utf8Json.Parse(ref jsonReader);
        }

        public static BitmapSource LoadImageFromFile(string fileName)
        {
            ArgumentNullException.ThrowIfNull(nameof(fileName));
            if (Path.GetExtension(fileName).Equals(".tga", StringComparison.OrdinalIgnoreCase))
            {
                var pfimImage = Pfim.Pfim.FromFile(fileName);
                var pinnedArray = GCHandle.Alloc(pfimImage.Data, GCHandleType.Pinned);
                var addr = pinnedArray.AddrOfPinnedObject();
                var bsource = BitmapSource.Create(pfimImage.Width, pfimImage.Height, 96.0, 96.0,
                    PixelFormat(pfimImage), null, addr, pfimImage.DataLen, pfimImage.Stride);
                return bsource;
            }
            else if (Path.GetExtension(fileName).Equals(".uasset", StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(fileName))
                {
                    using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    var result = UnrealThumbnail.LoadFromStream(stream);
                    if (result != null)
                        return result;
                }

                return NoImage;
            }
            else
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(fileName, UriKind.Absolute);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                return image;
            }
        }

        public static BitmapSource LoadImageFromStream(Stream stream, string ext)
        {
            ArgumentNullException.ThrowIfNull(nameof(stream));
            if (ext.Equals(".tga", StringComparison.OrdinalIgnoreCase))
            {
                var pfimImage = Pfim.Pfim.FromStream(stream);
                var pinnedArray = GCHandle.Alloc(pfimImage.Data, GCHandleType.Pinned);
                var addr = pinnedArray.AddrOfPinnedObject();
                var bsource = BitmapSource.Create(pfimImage.Width, pfimImage.Height, 96.0, 96.0,
                    PixelFormat(pfimImage), null, addr, pfimImage.DataLen, pfimImage.Stride);
                return bsource;
            }
            else if (ext.Equals(".uasset", StringComparison.OrdinalIgnoreCase))
            {
                var result = UnrealThumbnail.LoadFromStream(stream);
                if (result != null)
                    return result;
                return NoImage;
            }
            else
            {
                return BitmapFrame.Create(stream);
            }
        }

        public static readonly BitmapImage NoImage = new BitmapImage(new Uri("pack://application:,,,/Hercules;component/Resources/Misc/NoImage.png", UriKind.RelativeOrAbsolute));

        private static System.Windows.Media.PixelFormat PixelFormat(Pfim.IImage image)
        {
            switch (image.Format)
            {
                case Pfim.ImageFormat.Rgb24:
                    return System.Windows.Media.PixelFormats.Bgr24;
                case Pfim.ImageFormat.Rgba32:
                    return System.Windows.Media.PixelFormats.Bgr32;
                case Pfim.ImageFormat.Rgb8:
                    return System.Windows.Media.PixelFormats.Gray8;
                case Pfim.ImageFormat.R5g5b5a1:
                case Pfim.ImageFormat.R5g5b5:
                    return System.Windows.Media.PixelFormats.Bgr555;
                case Pfim.ImageFormat.R5g6b5:
                    return System.Windows.Media.PixelFormats.Bgr565;
                default:
                    throw new Exception($"Unable to convert {image.Format} to WPF PixelFormat");
            }
        }
    }
}
