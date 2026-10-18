using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Plugin.Maui.SignaturePad.Sample.ViewModels;

public enum ExportFormat
{
    Png,
    Jpeg,
    Svg,
}

/// <summary>The Export page: the export options, a preview of the result, and sharing it as a file.</summary>
public partial class ExportViewModel : ObservableObject
{
    private readonly IShare _share;
    private readonly IFileSystem _fileSystem;
    private Export? _export;

    public ExportViewModel(IShare share, IFileSystem fileSystem)
    {
        _share = share;
        _fileSystem = fileSystem;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportCommand))]
    public partial SignatureData Signature { get; set; } = SignatureData.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsImage))]
    public partial ExportFormat Format { get; set; }

    /// <summary>The pixels per device-independent unit of PNG and JPEG images.</summary>
    [ObservableProperty]
    public partial double Scale { get; set; } = 2;

    [ObservableProperty]
    public partial bool CropToContent { get; set; } = true;

    [ObservableProperty]
    public partial bool WhiteBackground { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasImage), nameof(HasResult))]
    public partial ImageSource? Image { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSvg), nameof(HasResult))]
    public partial string? Svg { get; private set; }

    [ObservableProperty]
    public partial string? ResultInfo { get; private set; }

    public bool IsImage => Format != ExportFormat.Svg;

    public bool HasImage => Image is not null;

    public bool HasSvg => Svg is not null;

    public bool HasResult => HasImage || HasSvg;

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportAsync(CancellationToken cancellationToken)
    {
        var background = WhiteBackground ? Colors.White : null;
        if (Format == ExportFormat.Svg)
        {
            var svg = Signature.ToSvg(new SignatureSvgExportOptions { CropToContent = CropToContent, BackgroundColor = background });
            Show(new Export(Encoding.UTF8.GetBytes(svg), "SVG", "svg", "image/svg+xml"));
            Svg = svg;
            return;
        }

        var options = new SignatureImageExportOptions
        {
            Format = Format == ExportFormat.Jpeg ? SignatureImageFormat.Jpeg : SignatureImageFormat.Png,
            Scale = (float)Scale,
            CropToContent = CropToContent,
            BackgroundColor = background,
        };
        var image = await Exports.ReadAllAsync(Signature.ToImageAsync(options, cancellationToken), cancellationToken);
        Show(Format == ExportFormat.Jpeg ? new Export(image, "JPEG", "jpg", "image/jpeg") : new Export(image, "PNG", "png", "image/png"));
        Image = Exports.ToImageSource(image);
    }

    [RelayCommand(CanExecute = nameof(CanShare))]
    private async Task ShareAsync(CancellationToken cancellationToken)
    {
        // Share a file from the cache directory, which the platform share sheets can read.
        var export = _export!;
        var path = Path.Combine(_fileSystem.CacheDirectory, "signature." + export.Extension);
        await File.WriteAllBytesAsync(path, export.Bytes, cancellationToken);
        await _share.RequestAsync(new ShareFileRequest("Signature", new ShareFile(path, export.ContentType)));
    }

    private bool CanExport() => !Signature.IsEmpty;

    private bool CanShare() => _export is not null;

    // The preview always matches the signature and the options, so a change removes it.
    partial void OnSignatureChanged(SignatureData value) => Show(null);

    partial void OnFormatChanged(ExportFormat value) => Show(null);

    partial void OnScaleChanged(double value) => Show(null);

    partial void OnCropToContentChanged(bool value) => Show(null);

    partial void OnWhiteBackgroundChanged(bool value) => Show(null);

    private void Show(Export? export)
    {
        _export = export;
        Image = null;
        Svg = null;
        ResultInfo = export is null ? null : export.Name + ", " + Exports.FormatSize(export.Bytes.Length);
        ShareCommand.NotifyCanExecuteChanged();
    }

    private sealed record Export(byte[] Bytes, string Name, string Extension, string ContentType);
}
