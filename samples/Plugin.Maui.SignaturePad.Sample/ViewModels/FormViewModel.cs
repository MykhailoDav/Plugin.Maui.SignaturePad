using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Plugin.Maui.SignaturePad.Sample.ViewModels;

/// <summary>
/// The Form page: a delivery receipt that can be submitted once it has a name, a confirmation and a signature. After
/// submitting, the form and the pad are read-only.
/// </summary>
public partial class FormViewModel : ObservableObject
{
    private readonly ISemanticScreenReader _screenReader;

    public FormViewModel(ISemanticScreenReader screenReader)
    {
        _screenReader = screenReader;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIncomplete))]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    public partial string Name { get; set; } = "";

    [ObservableProperty]
    public partial string Notes { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIncomplete))]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    public partial bool Confirmed { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIncomplete))]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    public partial SignatureData Signature { get; set; } = SignatureData.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSubmitted), nameof(IsEditable), nameof(IsIncomplete))]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    public partial ImageSource? SignatureImage { get; private set; }

    [ObservableProperty]
    public partial string? Receipt { get; private set; }

    public bool IsSubmitted => SignatureImage is not null;

    public bool IsEditable => !IsSubmitted;

    public bool IsIncomplete => IsEditable && !CanSubmit();

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync(CancellationToken cancellationToken)
    {
        // A real app would send the image, or the JSON from Signature.ToJson(), with the form.
        var png = await Exports.ReadAllAsync(Signature.ToImageAsync(cancellationToken: cancellationToken), cancellationToken);
        Receipt = string.Format(CultureInfo.CurrentCulture, "Signed by {0} at {1:t}.", Name.Trim(), DateTime.Now);
        SignatureImage = Exports.ToImageSource(png);
        _screenReader.Announce("Delivery confirmed.");
    }

    [RelayCommand]
    private void ToggleConfirmed()
    {
        if (IsEditable)
            Confirmed = !Confirmed;
    }

    [RelayCommand]
    private void NewDelivery()
    {
        Name = "";
        Notes = "";
        Confirmed = false;
        Signature = SignatureData.Empty;
        SignatureImage = null;
        Receipt = null;
    }

    private bool CanSubmit() => IsEditable && Confirmed && !Signature.IsEmpty && !string.IsNullOrWhiteSpace(Name);
}
