using Android.Graphics;
using Microsoft.Maui.Graphics.Platform;

namespace Plugin.Maui.SignaturePad;

internal static partial class SignatureImageExporter
{
    private static partial Task<Stream> ExportCoreAsync(
        SignatureData data, ExportLayout layout, SignatureImageExportOptions options, CancellationToken cancellationToken) =>
        Task.Run<Stream>(
            () =>
            {
                using var bitmap = Bitmap.CreateBitmap(layout.PixelWidth, layout.PixelHeight, Bitmap.Config.Argb8888!)
                    ?? throw new InvalidOperationException("Android could not create the bitmap.");
                using (var androidCanvas = new Canvas(bitmap))
                {
                    Render(new PlatformCanvas { Canvas = androidCanvas }, data, layout, options);
                }

                cancellationToken.ThrowIfCancellationRequested();

                var stream = new MemoryStream();
                var format = options.Format == SignatureImageFormat.Png ? Bitmap.CompressFormat.Png! : Bitmap.CompressFormat.Jpeg!;
                if (!bitmap.Compress(format, options.JpegQuality, stream))
                    throw new InvalidOperationException("Android could not encode the image.");

                stream.Position = 0;
                return stream;
            },
            cancellationToken);
}
