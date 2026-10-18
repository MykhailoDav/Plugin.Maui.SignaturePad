using CoreGraphics;
using Microsoft.Maui.Graphics.Platform;
using UIKit;

namespace Plugin.Maui.SignaturePad;

internal static partial class SignatureImageExporter
{
    private static partial Task<Stream> ExportCoreAsync(
        SignatureData data, ExportLayout layout, SignatureImageExportOptions options, CancellationToken cancellationToken) =>
        Task.Run<Stream>(
            () =>
            {
                using var colorSpace = CGColorSpace.CreateDeviceRGB();
                using var context = new CGBitmapContext(
                    null, layout.PixelWidth, layout.PixelHeight, 8, layout.PixelWidth * 4, colorSpace, CGImageAlphaInfo.PremultipliedLast);

                // CoreGraphics puts the origin at the bottom left; flip it so the drawing matches the pad.
                context.TranslateCTM(0, layout.PixelHeight);
                context.ScaleCTM(1, -1);
                Render(new PlatformCanvas(() => colorSpace) { Context = context }, data, layout, options);

                cancellationToken.ThrowIfCancellationRequested();

                using var cgImage = context.ToImage() ?? throw new InvalidOperationException("CoreGraphics could not create the image.");
                using var image = UIImage.FromImage(cgImage);
                using var encoded = (options.Format == SignatureImageFormat.Png ? image.AsPNG() : image.AsJPEG(options.JpegQuality / 100f))
                    ?? throw new InvalidOperationException("UIKit could not encode the image.");

                var stream = new MemoryStream(encoded.ToArray());
                return stream;
            },
            cancellationToken);
}
