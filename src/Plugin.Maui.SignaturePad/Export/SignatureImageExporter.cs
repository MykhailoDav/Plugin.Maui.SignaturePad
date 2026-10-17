namespace Plugin.Maui.SignaturePad;

/// <summary>
/// Draws a signature into a bitmap and encodes it. The layout and the checks are shared; each platform provides the
/// bitmap and the encoder in its own file.
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

    private static partial Task<Stream> ExportCoreAsync(
        SignatureData data, ExportLayout layout, SignatureImageExportOptions options, CancellationToken cancellationToken);
}
