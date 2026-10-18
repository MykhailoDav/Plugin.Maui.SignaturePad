using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Plugin.Maui.SignaturePad.Sample.ViewModels;

/// <summary>
/// The Data page: what a signature records, and saving it as JSON to restore it exactly later, for example after the
/// app restarts.
/// </summary>
public partial class DataViewModel : ObservableObject
{
    private const string FileName = "signature.json";
    private const int JsonPreviewLength = 1000;

    private readonly IFileSystem _fileSystem;
    private readonly IClipboard _clipboard;

    public DataViewModel(IFileSystem fileSystem, IClipboard clipboard)
    {
        _fileSystem = fileSystem;
        _clipboard = clipboard;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StrokeCount), nameof(PointCount), nameof(Duration), nameof(Devices), nameof(Json))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand), nameof(CopyCommand))]
    public partial SignatureData Signature { get; set; } = SignatureData.Empty;

    [ObservableProperty]
    public partial string? Status { get; private set; }

    public int StrokeCount => Signature.Strokes.Count;

    public int PointCount => Signature.PointCount;

    public string Duration => string.Format(CultureInfo.CurrentCulture, "{0:0.00} s", Signature.Duration / 1000);

    public string Devices
    {
        get
        {
            var devices = Signature.Strokes.Select(stroke => stroke.Device).Distinct().ToList();
            return devices.Count == 0 ? "None" : string.Join(", ", devices);
        }
    }

    /// <summary>The start of the JSON; a long signature has thousands of numbers.</summary>
    public string Json
    {
        get
        {
            var json = Signature.ToJson();
            return json.Length <= JsonPreviewLength ? json : string.Concat(json.AsSpan(0, JsonPreviewLength), "…");
        }
    }

    private string FilePath => Path.Combine(_fileSystem.AppDataDirectory, FileName);

    [RelayCommand(CanExecute = nameof(HasSignature))]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        var json = Signature.ToJson();
        await File.WriteAllTextAsync(FilePath, json, cancellationToken);
        Status = string.Format(CultureInfo.CurrentCulture, "Saved the signature as {0} ({1}).", FileName, Exports.FormatSize(json.Length));
        RestoreCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(HasSavedSignature))]
    private async Task RestoreAsync(CancellationToken cancellationToken)
    {
        var json = await File.ReadAllTextAsync(FilePath, cancellationToken);
        try
        {
            // Loading a signature replaces the one on the pad and forgets the pad's undo history.
            Signature = SignatureData.FromJson(json);
            Status = "Restored the signature from " + FileName + ".";
        }
        catch (FormatException exception)
        {
            Status = "The saved file is not a signature: " + exception.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(HasSignature))]
    private async Task CopyAsync()
    {
        await _clipboard.SetTextAsync(Signature.ToJson());
        Status = "Copied the JSON to the clipboard.";
    }

    private bool HasSignature() => !Signature.IsEmpty;

    private bool HasSavedSignature() => File.Exists(FilePath);
}
