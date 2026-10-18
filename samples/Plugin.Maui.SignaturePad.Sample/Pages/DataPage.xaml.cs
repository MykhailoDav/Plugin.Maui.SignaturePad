using Plugin.Maui.SignaturePad.Sample.ViewModels;

namespace Plugin.Maui.SignaturePad.Sample.Pages;

public partial class DataPage : ContentPage
{
    public DataPage(DataViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
