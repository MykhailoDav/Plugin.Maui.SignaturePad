using Plugin.Maui.SignaturePad.Rendering;

namespace Plugin.Maui.SignaturePad;

/// <summary>
/// Draws a signature into a bitmap and encodes it. The layout, the checks and the drawing are shared; each platform
/// provides only the bitmap and the encoder, in its own file.
/// </summary>
internal static partial class SignatureImageExporter
{
    public static Task<Stream> ExportAsync(SignatureData data, SignatureImageExportOptions options, CancellationToken cancellationToken)
    {
        var layout = ExportLayout.Compute(data, options.CropToContent, options.Padding, options.Scale);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<Stream>(cancellationToken);

        return ExportCoreAsync(data, layout, options, cancellationToken);
    }

    /// <summary>
    /// Draws the export on a canvas of <see cref="ExportLayout.PixelWidth"/> by <see cref="ExportLayout.PixelHeight"/>
    /// pixels, without canvas transforms: the background, then the strokes with the exported area moved to the origin and
    /// scaled to pixels.
    /// </summary>
    internal static void Render(ICanvas canvas, SignatureData data, ExportLayout layout, SignatureImageExportOptions options)
    {
        // JPEG has no transparency, and a transparent bitmap would encode as black.
        var background = options.BackgroundColor ?? (options.Format == SignatureImageFormat.Jpeg ? Colors.White : null);
        if (background is not null)
        {
            canvas.FillColor = background;
            canvas.FillRectangle(0, 0, layout.PixelWidth, layout.PixelHeight);
        }

        InkRenderer.DrawStrokes(canvas, data.Strokes, options.StrokeColor, layout.Source.Location, options.Scale);
    }

    private static partial Task<Stream> ExportCoreAsync(
        SignatureData data, ExportLayout layout, SignatureImageExportOptions options, CancellationToken cancellationToken);
}
