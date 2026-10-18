using Plugin.Maui.SignaturePad.Sample.ViewModels;

namespace Plugin.Maui.SignaturePad.Sample.Pages;

public partial class ExportPage : ContentPage
{
    public ExportPage(ExportViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
