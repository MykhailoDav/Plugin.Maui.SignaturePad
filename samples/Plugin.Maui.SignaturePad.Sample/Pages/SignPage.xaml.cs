using Plugin.Maui.SignaturePad.Sample.ViewModels;

namespace Plugin.Maui.SignaturePad.Sample.Pages;

public partial class SignPage : ContentPage
{
    public SignPage(SignViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
