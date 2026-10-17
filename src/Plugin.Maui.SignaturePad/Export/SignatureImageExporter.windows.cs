namespace Plugin.Maui.SignaturePad;

internal static partial class SignatureImageExporter
{
    // Replaced by the platform renderer in the image export task.
    private static partial Task<Stream> ExportCoreAsync(
        SignatureData data, ExportLayout layout, SignatureImageExportOptions options, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Image export is not available on this platform yet.");
}
