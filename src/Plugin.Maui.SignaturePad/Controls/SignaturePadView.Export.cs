namespace Plugin.Maui.SignaturePad;

public partial class SignaturePadView
{
    /// <summary>
    /// Exports the signature as a PNG or JPEG image, by default cropped to the ink and at 2 pixels per
    /// device-independent unit. The image is drawn off the UI thread with the same ink as the pad, without the
    /// placeholder and the signature line.
    /// </summary>
    /// <param name="options">The export options, or <see langword="null"/> for the defaults.</param>
    /// <param name="cancellationToken">Cancels the export.</param>
    /// <returns>A stream with the encoded image, positioned at its start. The caller disposes it.</returns>
    /// <exception cref="InvalidOperationException">The pad is empty, or the image would be too large.</exception>
    /// <exception cref="PlatformNotSupportedException">The code runs on a target without a platform (plain <c>net10.0</c>).</exception>
    public Task<Stream> ExportImageAsync(SignatureImageExportOptions? options = null, CancellationToken cancellationToken = default) =>
        SignatureImageExporter.ExportAsync(Signature, options ?? new SignatureImageExportOptions(), cancellationToken);

    /// <summary>Exports the signature as SVG; see <see cref="SignatureData.ToSvg"/>.</summary>
    /// <param name="options">The export options, or <see langword="null"/> for the defaults.</param>
    /// <returns>The SVG document.</returns>
    /// <exception cref="InvalidOperationException">The pad is empty.</exception>
    public string ExportSvg(SignatureSvgExportOptions? options = null) => Signature.ToSvg(options);
}
