namespace Plugin.Maui.SignaturePad;

internal static partial class SignatureImageExporter
{
    private static partial Task<Stream> ExportCoreAsync(
        SignatureData data, ExportLayout layout, SignatureImageExportOptions options, CancellationToken cancellationToken) =>
        throw new PlatformNotSupportedException(
            "Image export needs a platform (Android, iOS, Mac Catalyst or Windows). Without one, export with ToSvg or ToJson.");
}
