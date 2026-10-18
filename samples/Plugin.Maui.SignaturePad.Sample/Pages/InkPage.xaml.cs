using Plugin.Maui.SignaturePad.Sample.ViewModels;

namespace Plugin.Maui.SignaturePad.Sample.Pages;

public partial class InkPage : ContentPage
{
    public InkPage(InkViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
