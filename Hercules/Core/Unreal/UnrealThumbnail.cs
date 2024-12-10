using System.Buffers;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Unreal
{
    public static class UnrealThumbnail
    {
        public static BitmapSource? LoadFromStream(Stream stream)
        {
            using var reader = new BinaryReader(stream);
            var summary = new FPackageFileSummaryBinarySerializer().Deserialize(reader);
            stream.Seek(summary.ThumbnailTableOffset, SeekOrigin.Begin);
            var thumbnails = UnrealTypes.TArray(FThumbnailAssetDataBinarySerializer.Instance).Deserialize(reader);
            if (thumbnails.Count > 0)
            {
                stream.Seek(thumbnails[0].FileOffset, SeekOrigin.Begin);
                var thumbnail = FObjectThumbnailBinarySerializer.Instance.Deserialize(reader);
                if (thumbnail.ImageWidth > 0 && thumbnail.ImageHeight != 0 && thumbnail.CompressedImageData.Length > 0)
                {
                    var memoryStream = new MemoryStream();
                    memoryStream.Write(thumbnail.CompressedImageData);
                    memoryStream.Position = 0;
                    var image = (BitmapFrame)BitmapFrame.Create(memoryStream);
                    byte[] pixels = ArrayPool<byte>.Shared.Rent(image.PixelWidth * image.PixelHeight * 4);
                    image.CopyPixels(pixels, image.PixelWidth * 4, 0);
                    for (int i = 0; i < pixels.Length / 4; ++i)
                    {
                        byte b = pixels[i * 4];
                        byte r = pixels[i * 4 + 2];
                        pixels[i * 4 + 2] = b;
                        pixels[i * 4] = r;
                    }

                    // Write the modified pixels into a new bitmap and use that as the source of an Image
                    var bmp = new WriteableBitmap(image.PixelWidth, image.PixelHeight, image.DpiX, image.DpiY, PixelFormats.Bgra32, null);
                    bmp.WritePixels(new System.Windows.Int32Rect(0, 0, image.PixelWidth, image.PixelHeight), pixels, image.PixelWidth * 4, 0);
                    ArrayPool<byte>.Shared.Return(pixels);
                    return bmp;
                }
            }
            return null;
        }
    }
}
