using Microsoft.Graphics.Canvas;
using Microsoft.Maui.Graphics.Platform;
using Windows.Storage.Streams;

namespace Plugin.Maui.SignaturePad;

internal static partial class SignatureImageExporter
{
    // Maui.Graphics' own bitmap export is not implemented on Windows, so this draws into a Win2D render target directly,
    // as the CommunityToolkit's DrawingView does.
    private static partial Task<Stream> ExportCoreAsync(
        SignatureData data, ExportLayout layout, SignatureImageExportOptions options, CancellationToken cancellationToken) =>
        Task.Run<Stream>(
            async () =>
            {
                // 96 DPI makes one DIP of the render target one pixel; the scale is applied by Render.
                using var target = new CanvasRenderTarget(CanvasDevice.GetSharedDevice(), layout.PixelWidth, layout.PixelHeight, 96);
                using (var session = target.CreateDrawingSession())
                {
                    session.Clear(Microsoft.UI.Colors.Transparent);
                    Render(new PlatformCanvas { Session = session }, data, layout, options);
                }

                cancellationToken.ThrowIfCancellationRequested();

                using var encoded = new InMemoryRandomAccessStream();
                var format = options.Format == SignatureImageFormat.Png ? CanvasBitmapFileFormat.Png : CanvasBitmapFileFormat.Jpeg;
                await target.SaveAsync(encoded, format, options.JpegQuality / 100f);

                var stream = new MemoryStream();
                await encoded.AsStreamForRead().CopyToAsync(stream, cancellationToken);
                stream.Position = 0;
                return stream;
            },
            cancellationToken);
}
