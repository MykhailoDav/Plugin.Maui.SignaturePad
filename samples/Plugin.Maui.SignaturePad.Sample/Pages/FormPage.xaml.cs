using Plugin.Maui.SignaturePad.Sample.ViewModels;

namespace Plugin.Maui.SignaturePad.Sample.Pages;

public partial class FormPage : ContentPage
{
    public FormPage(FormViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
