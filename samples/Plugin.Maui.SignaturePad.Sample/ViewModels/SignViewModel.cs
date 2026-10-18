using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Plugin.Maui.SignaturePad.Sample.ViewModels;

/// <summary>
/// The Sign page: the signature is bound two-way to the pad, so the view model never touches the view, and saving it
/// as an image needs only the data.
/// </summary>
public partial class SignViewModel : ObservableObject
{
    /// <summary>The signature on the pad. The pad sets it after every stroke, undo, redo and clear.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial SignatureData Signature { get; set; } = SignatureData.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasImage))]
    public partial ImageSource? Image { get; private set; }

    [ObservableProperty]
    public partial string? ImageInfo { get; private set; }

    public bool HasImage => Image is not null;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        // A PNG cropped to the ink, with a transparent background, at 2 pixels per device-independent unit.
        var png = await Exports.ReadAllAsync(Signature.ToImageAsync(cancellationToken: cancellationToken), cancellationToken);
        Image = Exports.ToImageSource(png);
        ImageInfo = "PNG, " + Exports.FormatSize(png.Length);
    }

    private bool CanSave() => !Signature.IsEmpty;
}
